using LibraHub.BuildingBlocks.Caching;

namespace LibraHub.Catalog.Application.Books;

public static class CacheInvalidationHelper
{
    public static async Task InvalidateBookCacheAsync(
        ICache cache,
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var bookKey = CacheKeys.GetBookKey(bookId);
        await cache.RemoveAsync(bookKey, cancellationToken);

        var searchPattern = CacheKeys.GetSearchBooksPattern();
        await cache.RemoveByPatternAsync(searchPattern, cancellationToken);
    }
}
