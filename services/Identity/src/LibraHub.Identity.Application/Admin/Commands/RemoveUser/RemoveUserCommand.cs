using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Admin.Commands.RemoveUser;

public record RemoveUserCommand(Guid UserId, string Reason) : IRequest<Result>;
