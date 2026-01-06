using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Commands.CompleteRegistration;

public record CompleteRegistrationCommand(
    string Token,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? Phone = null) : IRequest<Result>;

