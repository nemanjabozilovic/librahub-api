using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LibraHub.BuildingBlocks.Caching;

public class StatisticsCacheHelper
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(5);

    public StatisticsCacheHelper(IDistributedCache cache)
    {
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

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
            // If deserialization fails, remove invalid cache entry
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
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultCacheExpiration
        };

        await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
    }

    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    public static string GetUserStatisticsKey() => "statistics:users";

    public static string GetBookStatisticsKey() => "statistics:books";

    public static string GetOrderStatisticsKey() => "statistics:orders";

    public static string GetEntitlementStatisticsKey() => "statistics:entitlements";

    public static string GetDashboardSummaryKey() => "statistics:dashboard:summary";
}
