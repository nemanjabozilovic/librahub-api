using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace LibraHub.BuildingBlocks.Caching;

public class RedisCache(IDistributedCache cache, IConnectionMultiplexer connectionMultiplexer) : ICache
{
    private readonly IDistributedCache _cache = cache;
    private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class
    {
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrEmpty(cachedData))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
        }
        catch
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string cacheKey,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var endpoints = _connectionMultiplexer.GetEndPoints();
        if (endpoints.Length == 0)
        {
            return;
        }

        var server = _connectionMultiplexer.GetServer(endpoints.First());
        var fullPattern = $"LibraHub:{pattern}";

        var keys = new List<RedisKey>();
        await foreach (var key in server.KeysAsync(pattern: fullPattern))
        {
            keys.Add(key);
        }

        if (keys.Count > 0)
        {
            await database.KeyDeleteAsync(keys.ToArray());
        }
    }
}
