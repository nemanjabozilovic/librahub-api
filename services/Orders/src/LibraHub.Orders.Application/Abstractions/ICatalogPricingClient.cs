using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Orders.Application.Abstractions;

public interface ICatalogPricingClient
{
    Task<Result<PricingQuote>> GetPricingQuoteAsync(
        List<Guid> bookIds,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}

public class PricingQuote
{
    public List<PricingQuoteItem> Items { get; init; } = new();
    public string Currency { get; init; } = string.Empty;
}

public class PricingQuoteItem
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
