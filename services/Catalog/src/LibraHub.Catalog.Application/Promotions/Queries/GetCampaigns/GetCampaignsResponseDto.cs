namespace LibraHub.Catalog.Application.Promotions.Queries.GetCampaigns;

public record GetCampaignsResponseDto
{
    public List<CampaignSummaryDto> Campaigns { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record CampaignSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public string StackingPolicy { get; init; } = string.Empty;
    public int Priority { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
