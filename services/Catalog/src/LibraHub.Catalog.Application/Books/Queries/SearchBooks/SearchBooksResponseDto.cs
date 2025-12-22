namespace LibraHub.Catalog.Application.Books.Queries.SearchBooks;

public record SearchBooksResponseDto
{
    public List<BookSummaryDto> Books { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record BookSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = new();
}
