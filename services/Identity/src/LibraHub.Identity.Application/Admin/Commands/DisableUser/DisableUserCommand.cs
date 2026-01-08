using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Admin.Commands.DisableUser;

public record DisableUserCommand(Guid UserId, string Reason) : IRequest<Result>;
