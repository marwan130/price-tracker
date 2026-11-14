namespace PriceTracker.Infrastructure.Authentication;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PriceTracker.Application.Interfaces.Services;

public class SecureTokenService : ISecureTokenService
{
    private readonly IConfiguration _config;

    public SecureTokenService(IConfiguration config)
        => _config = config;

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }

    public DateTime GetEmailVerificationExpiryUtc()
        => DateTime.UtcNow.AddHours(int.Parse(_config["Auth:EmailVerificationExpiryHours"] ?? "24"));

    public DateTime GetPasswordResetExpiryUtc()
        => DateTime.UtcNow.AddHours(int.Parse(_config["Auth:PasswordResetExpiryHours"] ?? "1"));
}
