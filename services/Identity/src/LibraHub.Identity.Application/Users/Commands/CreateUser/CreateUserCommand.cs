using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Domain.Users;
using MediatR;

namespace LibraHub.Identity.Application.Users.Commands.CreateUser;

public record CreateUserCommand(string Email, Role Role) : IRequest<Result<Guid>>;
