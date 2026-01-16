using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    DateTimeOffset DateOfBirth,
    bool EmailAnnouncementsEnabled,
    bool EmailPromotionsEnabled,
    string? Phone = null,
    string? RecaptchaToken = null) : IRequest<Result>;
