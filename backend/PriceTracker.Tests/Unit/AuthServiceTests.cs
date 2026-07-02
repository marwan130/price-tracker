namespace PriceTracker.Tests.Unit;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using PriceTracker.Application.DTOs.Auth;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Domain.Exceptions;
using PriceTracker.Infrastructure.Authentication;
using System;
using System.Threading.Tasks;
using Xunit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ISecureTokenService> _secureTokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _emailSenderMock = new Mock<IEmailSender>();
        _secureTokenServiceMock = new Mock<ISecureTokenService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "https://example.com"
            })
            .Build();

        _secureTokenServiceMock.Setup(x => x.GenerateToken()).Returns("verification_token");
        _secureTokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns<string>(token => $"hash_{token}");
        _secureTokenServiceMock.Setup(x => x.GetEmailVerificationExpiryUtc())
            .Returns(DateTime.UtcNow.AddHours(24));
        _secureTokenServiceMock.Setup(x => x.GetPasswordResetExpiryUtc())
            .Returns(DateTime.UtcNow.AddHours(1));

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _jwtTokenServiceMock.Object,
            _passwordHasherMock.Object,
            _emailSenderMock.Object,
            _secureTokenServiceMock.Object,
            configuration);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Test123!@#"
        };

        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(request.Email))
            .ReturnsAsync(false);

        _passwordHasherMock.Setup(x => x.Hash(request.Password))
            .Returns("hashed_password");

        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access_token");

        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.Name.Should().Be(request.Name);
        result.AccessToken.Should().BeEmpty();
        result.EmailVerified.Should().BeFalse();
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        _emailSenderMock.Verify(x => x.SendAsync(request.Email, It.IsAny<string>(), It.Is<string>(body => body.Contains("verify-email"))), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "existing@example.com",
            Password = "Test123!@#"
        };

        _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(request.Email))
            .ReturnsAsync(true);

        // Act
        var action = async () => await _authService.RegisterAsync(request);

        // Assert
        await action.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            Name = "Test User",
            PasswordHash = "hashed_password",
            Role = UserRole.User,
            IsActive = true,
            EmailVerified = true
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(x => x.Verify(request.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");
        
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ThrowsException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Test123!@#"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var action = async () => await _authService.LoginAsync(request);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!@#"
        };

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            Name = "Test User",
            PasswordHash = "correct_hash",
            Role = UserRole.User,
            IsActive = true,
            EmailVerified = true
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(x => x.Verify(request.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var action = async () => await _authService.LoginAsync(request);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid_refresh_token"
        };

        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hashed_password",
            Role = UserRole.User,
            IsActive = true,
            EmailVerified = true
        };

        _jwtTokenServiceMock.Setup(x => x.GetRefreshTokenUserId(request.RefreshToken))
            .Returns(userId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("new_access_token");
        
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid_token"
        };

        _jwtTokenServiceMock.Setup(x => x.GetRefreshTokenUserId(request.RefreshToken))
            .Returns((Guid?)null);

        // Act
        var action = async () => await _authService.RefreshTokenAsync(request);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }
}
