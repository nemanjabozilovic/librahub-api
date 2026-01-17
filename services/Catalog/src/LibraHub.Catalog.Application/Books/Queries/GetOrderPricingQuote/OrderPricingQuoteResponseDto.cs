namespace LibraHub.Catalog.Application.Books.Queries.GetOrderPricingQuote;

public record OrderPricingQuoteResponseDto
{
    public string Currency { get; init; } = string.Empty;
    public List<OrderPricingQuoteItemDto> Items { get; init; } = new();
}

public record OrderPricingQuoteItemDto
{
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public bool IsRemoved { get; init; }
    public decimal BasePrice { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal VatRate { get; init; }
    public Guid? PromotionId { get; init; }
    public string? PromotionName { get; init; }
    public decimal? DiscountAmount { get; init; }
}
