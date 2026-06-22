using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Library.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class EntitlementGrantedConsumer(
    INotificationRepository notificationRepository,
    INotificationSender notificationSender,
    IUserNotificationSettingsRepository settingsRepository,
    IIdentityClient identityClient,
    IInboxRepository inboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<EntitlementGrantedConsumer> logger)
{
    private const string EventType = EventTypes.EntitlementGrantedV1;

    public async Task HandleAsync(EntitlementGrantedV1 @event, CancellationToken cancellationToken)
    {
        var messageId = $"EntitlementGranted_{@event.UserId}_{@event.BookId}";

        logger.LogInformation("Processing EntitlementGranted event for UserId: {UserId}, BookId: {BookId}, MessageId: {MessageId}",
            @event.UserId, @event.BookId, messageId);

        if (await inboxRepository.IsProcessedAsync(messageId, cancellationToken))
        {
            logger.LogInformation("EntitlementGranted event already processed for MessageId: {MessageId}, UserId: {UserId}, BookId: {BookId}",
                messageId, @event.UserId, @event.BookId);
            return;
        }

        var userId = @event.UserId;

        var userSettings = await settingsRepository.GetByUserIdAsync(userId, cancellationToken);
        if (!NotificationConsumerHelper.ShouldReceiveNotifications(userSettings))
        {
            var isStaff = userSettings?.IsStaff ?? false;
            var isActive = userSettings?.IsActive ?? false;
            logger.LogInformation("User {UserId} should not receive notifications (staff: {IsStaff}, active: {IsActive}), skipping EntitlementGranted notification for BookId: {BookId}",
                userId, isStaff, isActive, @event.BookId);
            await inboxRepository.MarkAsProcessedAsync(messageId, EventType, cancellationToken);
            return;
        }

        var emailEnabled = userSettings!.EmailEnabled;
        var inAppEnabled = userSettings!.InAppEnabled;

        Notification? notification = null;

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                if (inAppEnabled)
                {
                    notification = new Notification(
                        Guid.NewGuid(),
                        userId,
                        NotificationType.EntitlementGranted,
                        NotificationMessages.EntitlementGranted.Title,
                        NotificationMessages.EntitlementGranted.GetMessage(@event.BookTitle, @event.BookId));

                    await notificationRepository.AddAsync(notification, ct);
                }

                await inboxRepository.MarkAsProcessedAsync(messageId, EventType, ct);
            }, cancellationToken);

            if (notification != null)
            {
                try
                {
                    await notificationSender.SendInAppAsync(userId, notification.Title, notification.Message, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send in-app notification to UserId: {UserId} for BookId: {BookId}",
                        userId, @event.BookId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process EntitlementGranted event for UserId: {UserId}, BookId: {BookId}, MessageId: {MessageId}",
                @event.UserId, @event.BookId, messageId);
            throw;
        }

        if (emailEnabled && !string.IsNullOrWhiteSpace(userSettings!.Email))
        {
            await NotificationConsumerHelper.SendTemplatedEmailIfActiveAsync(
                identityClient,
                notificationSender,
                logger,
                userId,
                NotificationMessages.EntitlementGranted.Title,
                "ENTITLEMENT_GRANTED",
                fullName => new
                {
                    FullName = fullName,
                    BookId = @event.BookId,
                    BookTitle = @event.BookTitle
                },
                cancellationToken);
        }

        logger.LogInformation("EntitlementGranted notification created for UserId: {UserId}, BookId: {BookId}", userId, @event.BookId);
    }
}
