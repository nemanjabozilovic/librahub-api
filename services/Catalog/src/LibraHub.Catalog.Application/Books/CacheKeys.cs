namespace LibraHub.Catalog.Application.Books;

public static class CacheKeys
{
    private const string BookPrefix = "book";
    private const string BooksSearchPrefix = "books:search";

    public static string GetBookKey(Guid bookId) => $"{BookPrefix}:{bookId}";

    public static string GetSearchBooksKey(string? searchTerm, int page, int pageSize, bool includeAllStatuses)
    {
        var term = string.IsNullOrWhiteSpace(searchTerm) ? "all" : searchTerm.ToLowerInvariant();
        return $"{BooksSearchPrefix}:{term}:page:{page}:size:{pageSize}:statuses:{includeAllStatuses}";
    }

    public static string GetBookPattern() => $"{BookPrefix}:*";

    public static string GetSearchBooksPattern() => $"{BooksSearchPrefix}:*";
}
