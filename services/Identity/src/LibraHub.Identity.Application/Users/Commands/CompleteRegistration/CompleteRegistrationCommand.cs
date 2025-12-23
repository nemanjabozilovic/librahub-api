using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Commands.CompleteRegistration;

public record CompleteRegistrationCommand(
    string Token,
    string FirstName,
    string LastName,
    string? Phone = null,
    DateTime? DateOfBirth = null) : IRequest<Result>;

