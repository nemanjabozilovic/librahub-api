using MediatR;

namespace LibraHub.Library.Application.Entitlements.Queries.GetAllEntitlements;

public class GetAllEntitlementsQuery : IRequest<LibraHub.BuildingBlocks.Results.Result<GetAllEntitlementsResponseDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? UserId { get; init; }
    public Guid? BookId { get; init; }
    public string? Status { get; init; } // "Active", "Revoked", or null for all
    public string? Source { get; init; } // "Purchase", "AdminGrant", "Promotion", or null for all
    public string? Period { get; init; } // "24h", "7d", "30d", or null for all
}
