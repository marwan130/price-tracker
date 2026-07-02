namespace PriceTracker.Infrastructure.Authentication;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Infrastructure.Persistence;

public class RefreshTokenStore
{
    private readonly ApplicationDbContext _context;
    private readonly ISecureTokenService  _secureTokenService;
    private readonly TimeSpan             _expiry;

    public RefreshTokenStore(
        ApplicationDbContext context,
        ISecureTokenService  secureTokenService,
        IConfiguration       config)
    {
        _context            = context;
        _secureTokenService = secureTokenService;
        _expiry             = TimeSpan.FromDays(int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "7"));
    }

    public virtual void Save(string refreshToken, Guid userId)
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            Token     = _secureTokenService.HashToken(refreshToken),
            UserId    = userId,
            ExpiresAt = DateTime.UtcNow.Add(_expiry),
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    public virtual Guid? Get(string refreshToken)
    {
        var tokenHash = _secureTokenService.HashToken(refreshToken);
        var row = _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefault(r => r.Token == tokenHash && r.ExpiresAt > DateTime.UtcNow);

        return row?.UserId;
    }

    public virtual void Revoke(string refreshToken)
    {
        var tokenHash = _secureTokenService.HashToken(refreshToken);
        var row = _context.RefreshTokens.FirstOrDefault(r => r.Token == tokenHash);
        if (row is null)
            return;

        _context.RefreshTokens.Remove(row);
        _context.SaveChanges();
    }

    public virtual void RevokeAllForUser(Guid userId)
    {
        _context.RefreshTokens
            .Where(r => r.UserId == userId)
            .ExecuteDelete();
    }

    public virtual bool IsValid(string refreshToken)
        => Get(refreshToken).HasValue;
}
