namespace PriceTracker.Infrastructure.Authentication;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PriceTracker.Domain.Entities;
using PriceTracker.Infrastructure.Persistence;

public class RefreshTokenStore
{
    private readonly ApplicationDbContext _context;
    private readonly TimeSpan             _expiry;

    public RefreshTokenStore(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _expiry  = TimeSpan.FromDays(int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "7"));
    }

    public void Save(string refreshToken, Guid userId)
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            Token     = refreshToken,
            UserId    = userId,
            ExpiresAt = DateTime.UtcNow.Add(_expiry),
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    public Guid? Get(string refreshToken)
    {
        var row = _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefault(r => r.Token == refreshToken && r.ExpiresAt > DateTime.UtcNow);

        return row?.UserId;
    }

    public void Revoke(string refreshToken)
    {
        var row = _context.RefreshTokens.FirstOrDefault(r => r.Token == refreshToken);
        if (row is null)
            return;

        _context.RefreshTokens.Remove(row);
        _context.SaveChanges();
    }

    public bool IsValid(string refreshToken)
        => Get(refreshToken).HasValue;
}
