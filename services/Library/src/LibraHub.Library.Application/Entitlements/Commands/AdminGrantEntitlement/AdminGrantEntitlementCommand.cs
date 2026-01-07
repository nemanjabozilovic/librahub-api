using MediatR;

namespace LibraHub.Library.Application.Entitlements.Commands.AdminGrantEntitlement;

public class AdminGrantEntitlementCommand : IRequest<LibraHub.BuildingBlocks.Results.Result<Guid>>
{
    public Guid UserId { get; init; }
    public Guid BookId { get; init; }
}
