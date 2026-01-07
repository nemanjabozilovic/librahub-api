namespace LibraHub.Catalog.Api.Dtos.Pricing;

public record PricingQuoteRequestDto
{
    public string Currency { get; init; } = BuildingBlocks.Constants.Currency.USD;
    public List<PricingQuoteItemRequestDto> Items { get; init; } = [];
    public DateTimeOffset? AtUtc { get; init; }
}
