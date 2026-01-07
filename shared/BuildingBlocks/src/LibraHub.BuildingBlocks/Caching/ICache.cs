namespace LibraHub.BuildingBlocks.Caching;

public interface ICache
{
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(
        string cacheKey,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default);

    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}
