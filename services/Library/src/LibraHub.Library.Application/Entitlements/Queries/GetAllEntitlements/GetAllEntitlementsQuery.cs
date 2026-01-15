using MediatR;

namespace LibraHub.Library.Application.Entitlements.Queries.GetAllEntitlements;

public class GetAllEntitlementsQuery : IRequest<LibraHub.BuildingBlocks.Results.Result<GetAllEntitlementsResponseDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? UserId { get; init; }
    public Guid? BookId { get; init; }
    public string? Status { get; init; }
    public string? Source { get; init; }
    public string? Period { get; init; }
}
