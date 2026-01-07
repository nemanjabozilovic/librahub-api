namespace LibraHub.Catalog.Application.Books.Dtos;

public record PricingDto
{
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? VatRate { get; init; }
    public decimal? PriceWithVat { get; init; }
    public decimal? PromoPrice { get; init; }
    public decimal? PromoPriceWithVat { get; init; }
    public DateTimeOffset? PromoStartDate { get; init; }
    public DateTimeOffset? PromoEndDate { get; init; }
}
