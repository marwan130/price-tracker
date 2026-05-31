namespace PriceTracker.Infrastructure.Authentication;

using Microsoft.Extensions.Caching.Memory;

public class RefreshTokenStore
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan     _expiry = TimeSpan.FromDays(7);

    public RefreshTokenStore(IMemoryCache cache)
        => _cache = cache;

    public void Save(string refreshToken, Guid userId)
        => _cache.Set(refreshToken, userId, _expiry);

    public Guid? Get(string refreshToken)
        => _cache.TryGetValue(refreshToken, out Guid userId) ? userId : null;

    public void Revoke(string refreshToken)
        => _cache.Remove(refreshToken);

    public bool IsValid(string refreshToken)
        => _cache.TryGetValue(refreshToken, out _);
}