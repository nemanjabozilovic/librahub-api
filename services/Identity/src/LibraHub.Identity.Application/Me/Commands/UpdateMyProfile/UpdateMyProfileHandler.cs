using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Me.Queries.GetMyProfile;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Me.Commands.UpdateMyProfile;

public class UpdateMyProfileHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IOutboxWriter outboxWriter,
    IClock clock,
    IOptions<IdentityOptions> identityOptions) : IRequestHandler<UpdateMyProfileCommand, Result<UserProfileDto>>
{
    private readonly IdentityOptions _identityOptions = identityOptions.Value;

    public async Task<Result<UserProfileDto>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId();
        if (userIdResult.IsFailure)
        {
            return Result.Failure<UserProfileDto>(userIdResult.Error!);
        }

        var user = await userRepository.GetByIdAsync(userIdResult.Value, cancellationToken);
        if (user == null)
        {
            return Result.Failure<UserProfileDto>(Error.NotFound("User not found"));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<UserProfileDto>(Error.Forbidden("Account is removed"));
        }

        user.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.DateOfBirth.UtcDateTime);

        user.SetEmailNotificationPreferences(request.EmailAnnouncementsEnabled, request.EmailPromotionsEnabled);

        await userRepository.UpdateAsync(user, cancellationToken);

        var settingsEvent = new UserNotificationSettingsChangedV1
        {
            UserId = user.Id,
            Email = user.Email,
            IsActive = user.Status == UserStatus.Active,
            IsStaff = user.IsStaff(),
            EmailAnnouncementsEnabled = user.EmailAnnouncementsEnabled,
            EmailPromotionsEnabled = user.EmailPromotionsEnabled,
            OccurredAt = clock.UtcNowOffset
        };

        await outboxWriter.WriteAsync(settingsEvent, EventTypes.UserNotificationSettingsChanged, cancellationToken);

        return Result.Success(new UserProfileDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = new DateTimeOffset(user.DateOfBirth, TimeSpan.Zero),
            Phone = user.Phone,
            Avatar = BuildAvatarUrl(user.Avatar, user.Id),
            EmailAnnouncementsEnabled = user.EmailAnnouncementsEnabled,
            EmailPromotionsEnabled = user.EmailPromotionsEnabled
        });
    }

    private string? BuildAvatarUrl(string? avatar, Guid userId)
    {
        var relative = NormalizeAvatarPath(avatar, userId);
        if (string.IsNullOrWhiteSpace(relative))
        {
            return null;
        }

        return $"{_identityOptions.GatewayBaseUrl.TrimEnd('/')}{relative}";
    }

    private static string? NormalizeAvatarPath(string? avatar, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(avatar))
        {
            return null;
        }

        var path = avatar.Trim();

        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                path = uri.AbsolutePath;
            }
        }

        var marker = "/avatar/";
        var idx = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        var after = path[(idx + marker.Length)..];
        var fileName = Path.GetFileName(after);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        return $"/api/users/{userId}/avatar/{fileName}";
    }
}

