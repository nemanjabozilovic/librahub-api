namespace LibraHub.Catalog.Application.Promotions.Queries.GetPricingQuote;

public record PricingQuoteResponseDto
{
    public string Currency { get; init; } = string.Empty;
    public List<PricingQuoteItemDto> Items { get; init; } = new();
}

public record PricingQuoteItemDto
{
    public Guid BookId { get; init; }
    public decimal BasePrice { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal VatRate { get; init; }
    public AppliedPromotionDto? AppliedPromotion { get; init; }
}

public record AppliedPromotionDto
{
    public string Name { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
}
