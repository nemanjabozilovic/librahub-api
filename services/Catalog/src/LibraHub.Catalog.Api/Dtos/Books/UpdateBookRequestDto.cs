namespace LibraHub.Catalog.Api.Dtos.Books;

public record UpdateBookRequestDto
{
    public string? Description { get; init; }
    public string? Language { get; init; }
    public string? Publisher { get; init; }
    public DateTimeOffset? PublicationDate { get; init; }
    public string? Isbn { get; init; }
    public List<string>? Authors { get; init; }
    public List<string>? Categories { get; init; }
    public List<string>? Tags { get; init; }
}
