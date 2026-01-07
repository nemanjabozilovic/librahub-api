using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Library.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class EntitlementGrantedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
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

        var preference = await preferencesRepository.GetByUserIdAndTypeAsync(
            userId,
            NotificationType.EntitlementGranted,
            cancellationToken);

        var emailEnabled = preference?.EmailEnabled ?? true;
        var inAppEnabled = preference?.InAppEnabled ?? true;

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
                        NotificationMessages.EntitlementGranted.GetMessage(@event.BookId));

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

        if (emailEnabled)
        {
            try
            {
                var userInfo = await identityClient.GetUserInfoAsync(userId, cancellationToken);

                if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email) && userInfo.IsActive)
                {
                    var emailSubject = NotificationMessages.EntitlementGranted.Title;
                    var fullName = !string.IsNullOrWhiteSpace(userInfo.FullName)
                        ? userInfo.FullName
                        : userInfo.Email.Split('@')[0];

                    var emailModel = new
                    {
                        FullName = fullName,
                        BookId = @event.BookId
                    };
                    await notificationSender.SendEmailWithTemplateAsync(
                        userInfo.Email,
                        emailSubject,
                        "ENTITLEMENT_GRANTED",
                        emailModel,
                        cancellationToken);
                }
                else
                {
                    logger.LogWarning("User info not found, inactive, or email not available for UserId: {UserId}, skipping email notification", userId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email notification to UserId: {UserId} for BookId: {BookId}",
                    userId, @event.BookId);
            }
        }

        logger.LogInformation("EntitlementGranted notification created for UserId: {UserId}, BookId: {BookId}", userId, @event.BookId);
    }
}
