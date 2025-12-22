namespace LibraHub.Catalog.Application.Books.Queries.GetBook;

public record GetBookResponseDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Language { get; init; }
    public string? Publisher { get; init; }
    public DateTime? PublicationDate { get; init; }
    public string? Isbn { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = new();
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public PricingDto? Pricing { get; init; }
}

public record PricingDto
{
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? VatRate { get; init; }
    public decimal? PromoPrice { get; init; }
    public DateTime? PromoStartDate { get; init; }
    public DateTime? PromoEndDate { get; init; }
}
