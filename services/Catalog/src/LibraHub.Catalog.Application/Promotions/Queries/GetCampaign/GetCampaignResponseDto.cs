namespace LibraHub.Catalog.Application.Promotions.Queries.GetCampaign;

public record GetCampaignResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public string StackingPolicy { get; init; } = string.Empty;
    public int Priority { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<PromotionRuleDto> Rules { get; init; } = new();
}

public record PromotionRuleDto
{
    public Guid Id { get; init; }
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public string? Currency { get; init; }
    public decimal? MinPriceAfterDiscount { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public string AppliesToScope { get; init; } = string.Empty;
    public List<string>? ScopeValues { get; init; }
    public List<Guid>? Exclusions { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
