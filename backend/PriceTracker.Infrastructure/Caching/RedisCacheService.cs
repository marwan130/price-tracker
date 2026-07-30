namespace PriceTracker.Infrastructure.Caching;

using System.Text.Json;
using StackExchange.Redis;
using PriceTracker.Application.Interfaces.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase              _database;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis    = redis;
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (RedisException)
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            if (expiry.HasValue)
                await _database.StringSetAsync(key, serialized, expiry.Value);
            else
                await _database.StringSetAsync(key, serialized);
        }
        catch (RedisException)
        {
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (RedisException)
        {
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys   = server.KeysAsync(pattern: $"{prefix}*");

            var batch = new List<RedisKey>();
            await foreach (var key in keys)
                batch.Add(key);

            if (batch.Count > 0)
                await _database.KeyDeleteAsync([.. batch]);
        }
        catch (RedisException)
        {
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (RedisException)
        {
            return false;
        }
    }
}