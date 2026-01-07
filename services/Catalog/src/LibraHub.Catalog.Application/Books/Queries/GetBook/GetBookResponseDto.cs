using LibraHub.Catalog.Application.Books.Dtos;

namespace LibraHub.Catalog.Application.Books.Queries.GetBook;

public record GetBookResponseDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Language { get; init; }
    public string? Publisher { get; init; }
    public DateTimeOffset? PublicationDate { get; init; }
    public string? Isbn { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = new();
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public PricingDto? Pricing { get; init; }
    public string? CoverUrl { get; init; }
    public bool HasEdition { get; init; }
    public List<EditionDto> Editions { get; init; } = new();
}
