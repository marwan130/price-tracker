namespace PriceTracker.Infrastructure.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly RefreshTokenStore _refreshTokenStore;

    public JwtTokenService(IConfiguration config, RefreshTokenStore refreshTokenStore)
    {
        _config = config;
        _refreshTokenStore = refreshTokenStore;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.Name),
            new Claim(ClaimTypes.Role,           user.Role.ToString())
        };

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenExpiryMinutes"]!));

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public void SaveRefreshToken(string refreshToken, Guid userId)
        => _refreshTokenStore.Save(refreshToken, userId);

    public Guid? GetRefreshTokenUserId(string refreshToken)
        => _refreshTokenStore.Get(refreshToken);

    public void RevokeRefreshToken(string refreshToken)
        => _refreshTokenStore.Revoke(refreshToken);

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var handler    = new JwtSecurityTokenHandler();
            var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = true,
                ValidIssuer              = _config["Jwt:Issuer"],
                ValidateAudience         = true,
                ValidAudience            = _config["Jwt:Audience"],
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            var claim     = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(claim, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }
}