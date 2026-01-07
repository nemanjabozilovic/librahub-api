namespace LibraHub.Catalog.Api.Dtos.Books;

public record SetPricingRequestDto
{
    public decimal Price { get; init; }
    public string Currency { get; init; } = BuildingBlocks.Constants.Currency.USD;
    public decimal? VatRate { get; init; }
    public decimal? PromoPrice { get; init; }
    public DateTimeOffset? PromoStartDate { get; init; }
    public DateTimeOffset? PromoEndDate { get; init; }
}
