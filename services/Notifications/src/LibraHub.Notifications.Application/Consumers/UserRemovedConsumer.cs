using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class UserRemovedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository notificationPreferencesRepository,
    IUserNotificationSettingsRepository settingsRepository,
    IUnitOfWork unitOfWork,
    ILogger<UserRemovedConsumer> logger)
{
    public async Task HandleAsync(UserRemovedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing UserRemoved event for UserId: {UserId}, Reason: {Reason}", @event.UserId, @event.Reason);

        var notifications = await notificationRepository.GetAllByUserIdAsync(@event.UserId, cancellationToken);

        var preferences = await notificationPreferencesRepository.GetByUserIdAsync(@event.UserId, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var notification in notifications)
            {
                await notificationRepository.DeleteAsync(notification, ct);
            }

            foreach (var preference in preferences)
            {
                await notificationPreferencesRepository.DeleteAsync(preference, ct);
            }

            await settingsRepository.DeleteAsync(@event.UserId, ct);
        }, cancellationToken);

        logger.LogInformation("Deleted {NotificationCount} notifications and {PreferenceCount} preferences for UserId: {UserId}",
            notifications.Count, preferences.Count, @event.UserId);
    }
}

