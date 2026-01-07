using MediatR;

namespace LibraHub.Library.Application.Entitlements.Queries.CheckAccess;

public class CheckAccessQuery : IRequest<LibraHub.BuildingBlocks.Results.Result<CheckAccessDto>>
{
    public Guid UserId { get; init; }
    public Guid BookId { get; init; }
}
