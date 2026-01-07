using MediatR;

namespace LibraHub.Library.Application.Entitlements.Commands.RevokeEntitlement;

public class RevokeEntitlementCommand : IRequest<LibraHub.BuildingBlocks.Results.Result>
{
    public Guid EntitlementId { get; init; }
    public string? Reason { get; init; }
}
