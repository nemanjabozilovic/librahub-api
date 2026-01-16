using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class OrderRefundedConsumer(
    INotificationRepository notificationRepository,
    INotificationSender notificationSender,
    IUserNotificationSettingsRepository settingsRepository,
    IIdentityClient identityClient,
    IInboxRepository inboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<OrderRefundedConsumer> logger)
{
    private const string EventType = EventTypes.OrderRefundedV1;

    public async Task HandleAsync(OrderRefundedV1 @event, CancellationToken cancellationToken)
    {
        var messageId = $"OrderRefunded_{@event.OrderId}";

        logger.LogInformation("Processing OrderRefunded event for OrderId: {OrderId}, UserId: {UserId}, MessageId: {MessageId}",
            @event.OrderId, @event.UserId, messageId);

        if (await inboxRepository.IsProcessedAsync(messageId, cancellationToken))
        {
            logger.LogInformation("OrderRefunded event already processed for MessageId: {MessageId}, OrderId: {OrderId}",
                messageId, @event.OrderId);
            return;
        }

        var userId = @event.UserId;

        var userSettings = await settingsRepository.GetByUserIdAsync(userId, cancellationToken);
        if (!NotificationConsumerHelper.ShouldReceiveNotifications(userSettings))
        {
            var isStaff = userSettings?.IsStaff ?? false;
            var isActive = userSettings?.IsActive ?? false;
            logger.LogInformation("User {UserId} should not receive notifications (staff: {IsStaff}, active: {IsActive}), skipping OrderRefunded notification for OrderId: {OrderId}",
                userId, isStaff, isActive, @event.OrderId);
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
                        NotificationType.OrderRefunded,
                        NotificationMessages.OrderRefunded.Title,
                        NotificationMessages.OrderRefunded.GetMessage(@event.OrderId, @event.Reason));

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
                    logger.LogError(ex, "Failed to send in-app notification to UserId: {UserId} for OrderId: {OrderId}",
                        userId, @event.OrderId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process OrderRefunded event for OrderId: {OrderId}, MessageId: {MessageId}",
                @event.OrderId, messageId);
            throw;
        }

        if (emailEnabled && !string.IsNullOrWhiteSpace(userSettings!.Email))
        {
            try
            {
                var userInfoResult = await identityClient.GetUserInfoAsync(userId, cancellationToken);
                var userInfo = userInfoResult.IsSuccess ? userInfoResult.Value : null;

                if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email) && userInfo.IsActive)
                {
                    var emailSubject = NotificationMessages.OrderRefunded.Title;
                    var fullName = !string.IsNullOrWhiteSpace(userInfo.FullName)
                        ? userInfo.FullName
                        : userInfo.Email.Split('@')[0];

                    var emailModel = new
                    {
                        FullName = fullName,
                        OrderId = @event.OrderId,
                        Reason = @event.Reason
                    };
                    await notificationSender.SendEmailWithTemplateAsync(
                        userInfo.Email,
                        emailSubject,
                        "ORDER_REFUNDED",
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
                logger.LogError(ex, "Failed to send email notification to UserId: {UserId} for OrderId: {OrderId}",
                    userId, @event.OrderId);
            }
        }

        logger.LogInformation("OrderRefunded notification created for UserId: {UserId}, OrderId: {OrderId}", userId, @event.OrderId);
    }
}
