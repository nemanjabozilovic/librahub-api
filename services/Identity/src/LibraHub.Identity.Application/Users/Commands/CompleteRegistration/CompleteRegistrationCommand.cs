using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Commands.CompleteRegistration;

public record CompleteRegistrationCommand(
    string Token,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    DateTimeOffset DateOfBirth,
    bool EmailAnnouncementsEnabled,
    bool EmailPromotionsEnabled,
    string? Phone = null) : IRequest<Result>;
