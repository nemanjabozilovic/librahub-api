using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Commands.UpdateNotificationSettings;

public class UpdateNotificationSettingsHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IOutboxWriter outboxWriter,
    IClock clock) : IRequestHandler<UpdateNotificationSettingsCommand, Result>
{
    public async Task<Result> Handle(UpdateNotificationSettingsCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId("Not authenticated");
        if (userIdResult.IsFailure)
        {
            return userIdResult;
        }

        if (!request.EmailAnnouncementsEnabled.HasValue && !request.EmailPromotionsEnabled.HasValue)
        {
            return Result.Failure(Error.Validation("At least one setting must be provided"));
        }

        var userId = userIdResult.Value;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        var announcements = request.EmailAnnouncementsEnabled ?? user.EmailAnnouncementsEnabled;
        var promotions = request.EmailPromotionsEnabled ?? user.EmailPromotionsEnabled;

        user.SetEmailNotificationPreferences(announcements, promotions);

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

        return Result.Success();
    }
}
