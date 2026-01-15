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
    public string? Description { get; init; }
    public string Authors { get; init; } = string.Empty;
    public string? CoverUrl { get; init; }
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public bool HasEdition { get; init; }
    public List<BookEditionDto> Editions { get; init; } = new();
    public DateTimeOffset AcquiredAt { get; init; }
}

public class BookEditionDto
{
    public Guid Id { get; init; }
    public string Format { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
