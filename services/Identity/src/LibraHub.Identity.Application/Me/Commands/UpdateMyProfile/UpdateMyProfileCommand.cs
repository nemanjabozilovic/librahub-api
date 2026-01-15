using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Me.Queries.GetMyProfile;
using MediatR;

namespace LibraHub.Identity.Application.Me.Commands.UpdateMyProfile;

public record UpdateMyProfileCommand(
    string FirstName,
    string LastName,
    DateTimeOffset DateOfBirth,
    string? Phone,
    bool EmailAnnouncementsEnabled,
    bool EmailPromotionsEnabled) : IRequest<Result<UserProfileDto>>;

