namespace LibraHub.Catalog.Api.Dtos.Promotions;

public record CreatePromotionCampaignRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public string StackingPolicy { get; init; } = string.Empty;
    public int Priority { get; init; }
}
