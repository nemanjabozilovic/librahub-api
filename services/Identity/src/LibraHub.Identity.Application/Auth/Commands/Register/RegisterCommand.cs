using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone = null,
    DateTime? DateOfBirth = null) : IRequest<Result<Guid>>;
