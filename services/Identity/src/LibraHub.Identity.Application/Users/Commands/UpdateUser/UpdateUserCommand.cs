using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Users.Queries.GetUsers;
using MediatR;

namespace LibraHub.Identity.Application.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? Phone = null,
    bool? EmailVerified = null) : IRequest<Result<UserDto>>;

