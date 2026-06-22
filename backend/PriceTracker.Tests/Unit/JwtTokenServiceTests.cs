namespace PriceTracker.Tests.Unit;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Infrastructure.Authentication;
using System;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<RefreshTokenStore> _refreshTokenStoreMock;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Secret"]).Returns("AILqgTyeEND5qNdLIQOiWOjvJrJPCGQzgxvs0vYn0zxSZSByXZ7G5CHk+mVbQxsBc+Pz38NF8FR07CiDXDUptQ==");
        _configMock.Setup(c => c["Jwt:AccessTokenExpiryMinutes"]).Returns("15");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("SmartPriceTracker");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("SmartPriceTrackerUsers");
        _configMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        _refreshTokenStoreMock = new Mock<RefreshTokenStore>(null!, _configMock.Object);

        _jwtTokenService = new JwtTokenService(_configMock.Object, _refreshTokenStoreMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_WithValidClaims_ReturnsToken()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            Role = UserRole.User
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == user.UserId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == user.Role.ToString());
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsToken()
    {
        // Act
        var token = _jwtTokenService.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().NotBe(_jwtTokenService.GenerateRefreshToken()); // Tokens should be unique
    }

    [Fact]
    public void GetUserIdFromToken_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            Role = UserRole.User
        };
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Act
        var extractedUserId = _jwtTokenService.GetUserIdFromToken(token);

        // Assert
        extractedUserId.Should().Be(user.UserId);
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid_token_string";

        // Act
        var extractedUserId = _jwtTokenService.GetUserIdFromToken(invalidToken);

        // Assert
        extractedUserId.Should().BeNull();
    }
}
