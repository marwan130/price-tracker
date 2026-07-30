namespace PriceTracker.Infrastructure.Caching;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using PriceTracker.Application.Interfaces.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase              _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis    = redis;
        _database = redis.GetDatabase();
        _logger   = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key '{Key}' — returning cache miss", key);
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
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key '{Key}' — skipping cache write", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis DEL failed for key '{Key}'", key);
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
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis RemoveByPrefix failed for prefix '{Prefix}'", prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis EXISTS failed for key '{Key}' — returning false", key);
            return false;
        }
    }
}