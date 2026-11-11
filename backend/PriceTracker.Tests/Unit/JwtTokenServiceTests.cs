namespace PriceTracker.Tests.Unit;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Enums;
using PriceTracker.Infrastructure.Authentication;
using PriceTracker.Application.Interfaces.Services;
using System;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IRefreshTokenStore> _refreshTokenStoreMock;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Secret"]).Returns("AILqgTyeEND5qNdLIQOiWOjvJrJPCGQzgxvs0vYn0zxSZSByXZ7G5CHk+mVbQxsBc+Pz38NF8FR07CiDXDUptQ==");
        _configMock.Setup(c => c["Jwt:AccessTokenExpiryMinutes"]).Returns("15");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("SmartPriceTracker");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("SmartPriceTrackerUsers");
        _configMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        _refreshTokenStoreMock = new Mock<IRefreshTokenStore>();

        _jwtTokenService = new JwtTokenService(_configMock.Object, _refreshTokenStoreMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_WithValidClaims_ReturnsToken()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            Role = UserRole.User
        };
        var token = _jwtTokenService.GenerateAccessToken(user);
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
        var token = _jwtTokenService.GenerateRefreshToken();
        token.Should().NotBeNullOrEmpty();
        token.Should().NotBe(_jwtTokenService.GenerateRefreshToken());
    }

    [Fact]
    public void GetUserIdFromToken_WithValidToken_ReturnsUserId()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            Role = UserRole.User
        };
        var token = _jwtTokenService.GenerateAccessToken(user);
        var extractedUserId = _jwtTokenService.GetUserIdFromToken(token);
        extractedUserId.Should().Be(user.UserId);
    }

    [Fact]
    public void GetUserIdFromToken_WithInvalidToken_ReturnsNull()
    {
        var invalidToken = "invalid_token_string";
        var extractedUserId = _jwtTokenService.GetUserIdFromToken(invalidToken);
        extractedUserId.Should().BeNull();
    }
}
