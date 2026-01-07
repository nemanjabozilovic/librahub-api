namespace LibraHub.Catalog.Api.Dtos.Books;

public record CreateBookRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public DateTimeOffset PublicationDate { get; init; }
    public string Isbn { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = new();
    public List<string> Categories { get; init; } = new();
    public List<string>? Tags { get; init; }
}
