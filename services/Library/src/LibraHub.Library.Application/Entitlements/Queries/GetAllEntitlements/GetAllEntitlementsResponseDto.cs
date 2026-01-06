namespace LibraHub.Library.Application.Entitlements.Queries.GetAllEntitlements;

public class GetAllEntitlementsResponseDto
{
    public List<EntitlementSummaryDto> Entitlements { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class EntitlementSummaryDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? UserDisplayName { get; init; }
    public string? UserEmail { get; init; }
    public Guid BookId { get; init; }
    public string? BookTitle { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTime AcquiredAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public string? RevocationReason { get; init; }
    public Guid? OrderId { get; init; }
}

