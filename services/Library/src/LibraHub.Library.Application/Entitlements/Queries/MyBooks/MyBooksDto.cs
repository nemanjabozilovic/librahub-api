namespace LibraHub.Library.Application.Entitlements.Queries.MyBooks;

public class MyBooksDto
{
    public List<BookDto> Books { get; init; } = new();
    public int TotalCount { get; init; }
}

public class BookDto
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public string? CoverRef { get; init; }
    public DateTimeOffset AcquiredAt { get; init; }
}
