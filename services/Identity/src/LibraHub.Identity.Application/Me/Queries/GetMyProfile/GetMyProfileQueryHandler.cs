using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Me.Queries.GetMyProfile;

public class GetMyProfileQueryHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IOptions<IdentityOptions> identityOptions,
    ILogger<GetMyProfileQueryHandler> logger) : IRequestHandler<GetMyProfileQuery, Result<UserProfileDto>>
{
    private readonly IdentityOptions _identityOptions = identityOptions.Value;

    public async Task<Result<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId();
        if (userIdResult.IsFailure)
        {
            return Result.Failure<UserProfileDto>(userIdResult.Error!);
        }

        var user = await userRepository.GetByIdAsync(userIdResult.Value, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found: {UserId}", userIdResult.Value);
            return Result.Failure<UserProfileDto>(Error.Unauthorized("User not found"));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<UserProfileDto>(Error.Forbidden("Account is removed"));
        }

        return Result.Success(new UserProfileDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = new DateTimeOffset(user.DateOfBirth, TimeSpan.Zero),
            Phone = user.Phone,
            Avatar = AvatarUrlHelper.BuildAvatarUrl(user.Avatar, user.Id, _identityOptions.GatewayBaseUrl),
            EmailAnnouncementsEnabled = user.EmailAnnouncementsEnabled,
            EmailPromotionsEnabled = user.EmailPromotionsEnabled
        });
    }
}
