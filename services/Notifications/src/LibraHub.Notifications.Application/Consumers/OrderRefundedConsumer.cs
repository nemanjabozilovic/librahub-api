using LibraHub.Contracts.Orders.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class OrderRefundedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
    IIdentityClient identityClient,
    ILogger<OrderRefundedConsumer> logger)
{
    public async Task HandleAsync(OrderRefundedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing OrderRefunded event for OrderId: {OrderId}, UserId: {UserId}",
            @event.OrderId, @event.UserId);

        var userId = @event.UserId;

        // Check user preferences
        var preference = await preferencesRepository.GetByUserIdAndTypeAsync(
            userId,
            NotificationType.OrderRefunded,
            cancellationToken);

        var emailEnabled = preference?.EmailEnabled ?? true;
        var inAppEnabled = preference?.InAppEnabled ?? true;

        if (inAppEnabled)
        {
            // Create in-app notification
            var notification = new Notification(
                Guid.NewGuid(),
                userId,
                NotificationType.OrderRefunded,
                "Your order has been refunded",
                $"Order #{@event.OrderId} has been refunded. Reason: {@event.Reason}");

            await notificationRepository.AddAsync(notification, cancellationToken);
            await notificationSender.SendInAppAsync(userId, notification.Title, notification.Message, cancellationToken);
        }

        if (emailEnabled)
        {
            // Get user info from Identity service
            var userInfo = await identityClient.GetUserInfoAsync(userId, cancellationToken);

            if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email))
            {
                var emailSubject = "Order Refund Confirmation";
                var emailModel = new
                {
                    FullName = !string.IsNullOrWhiteSpace(userInfo.FullName) ? userInfo.FullName : $"User {userId}",
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
                logger.LogWarning("User info not found or email not available for UserId: {UserId}, skipping email notification", userId);
            }
        }

        logger.LogInformation("OrderRefunded notification created for UserId: {UserId}, OrderId: {OrderId}", userId, @event.OrderId);
    }
}

