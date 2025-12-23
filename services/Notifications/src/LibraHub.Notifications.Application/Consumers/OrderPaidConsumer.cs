using LibraHub.Contracts.Orders.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class OrderPaidConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
    IIdentityClient identityClient,
    ILogger<OrderPaidConsumer> logger)
{
    public async Task HandleAsync(OrderPaidV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing OrderPaid event for OrderId: {OrderId}, UserId: {UserId}",
            @event.OrderId, @event.UserId);

        var userId = @event.UserId;

        // Check user preferences
        var preference = await preferencesRepository.GetByUserIdAndTypeAsync(
            userId,
            NotificationType.OrderPaid,
            cancellationToken);

        var emailEnabled = preference?.EmailEnabled ?? true;
        var inAppEnabled = preference?.InAppEnabled ?? true;

        if (inAppEnabled)
        {
            // Create in-app notification
            var notification = new Notification(
                Guid.NewGuid(),
                userId,
                NotificationType.OrderPaid,
                "Your order has been paid",
                $"Order #{@event.OrderId} has been successfully paid. Total: {@event.Total} {@event.Currency}");

            await notificationRepository.AddAsync(notification, cancellationToken);
            await notificationSender.SendInAppAsync(userId, notification.Title, notification.Message, cancellationToken);
        }

        if (emailEnabled)
        {
            // Get user info from Identity service
            var userInfo = await identityClient.GetUserInfoAsync(userId, cancellationToken);

            if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email))
            {
                var emailSubject = "Order Payment Confirmation";
                var emailModel = new
                {
                    FullName = !string.IsNullOrWhiteSpace(userInfo.FullName) ? userInfo.FullName : $"User {userId}",
                    OrderId = @event.OrderId,
                    Total = @event.Total,
                    Currency = @event.Currency
                };
                await notificationSender.SendEmailWithTemplateAsync(
                    userInfo.Email,
                    emailSubject,
                    "ORDER_PAID",
                    emailModel,
                    cancellationToken);
            }
            else
            {
                logger.LogWarning("User info not found or email not available for UserId: {UserId}, skipping email notification", userId);
            }
        }

        logger.LogInformation("OrderPaid notification created for UserId: {UserId}, OrderId: {OrderId}", userId, @event.OrderId);
    }
}

