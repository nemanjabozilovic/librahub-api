using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class UserRemovedConsumer(
    INotificationRepository notificationRepository,
    IUserNotificationSettingsRepository settingsRepository,
    IUnitOfWork unitOfWork,
    ILogger<UserRemovedConsumer> logger)
{
    public async Task HandleAsync(UserRemovedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing UserRemoved event for UserId: {UserId}, Reason: {Reason}", @event.UserId, @event.Reason);

        var notifications = await notificationRepository.GetAllByUserIdAsync(@event.UserId, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var notification in notifications)
            {
                await notificationRepository.DeleteAsync(notification, ct);
            }

            await settingsRepository.DeleteAsync(@event.UserId, ct);
        }, cancellationToken);

        logger.LogInformation("Deleted {NotificationCount} notifications and user settings for UserId: {UserId}",
            notifications.Count, @event.UserId);
    }
}

