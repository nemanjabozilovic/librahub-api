using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class OrderPaidConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
    IIdentityClient identityClient,
    IInboxRepository inboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<OrderPaidConsumer> logger)
{
    private const string EventType = EventTypes.OrderPaidV1;

    public async Task HandleAsync(OrderPaidV1 @event, CancellationToken cancellationToken)
    {
        var messageId = $"OrderPaid_{@event.OrderId}";

        logger.LogInformation("Processing OrderPaid event for OrderId: {OrderId}, UserId: {UserId}, MessageId: {MessageId}",
            @event.OrderId, @event.UserId, messageId);

        if (await inboxRepository.IsProcessedAsync(messageId, cancellationToken))
        {
            logger.LogInformation("OrderPaid event already processed for MessageId: {MessageId}, OrderId: {OrderId}",
                messageId, @event.OrderId);
            return;
        }

        var userId = @event.UserId;

        var preference = await preferencesRepository.GetByUserIdAndTypeAsync(
            userId,
            NotificationType.OrderPaid,
            cancellationToken);

        var emailEnabled = preference?.EmailEnabled ?? false;
        var inAppEnabled = preference?.InAppEnabled ?? false;

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
                        NotificationType.OrderPaid,
                        NotificationMessages.OrderPaid.Title,
                        NotificationMessages.OrderPaid.GetMessage(@event.OrderId, @event.Total, @event.Currency));

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
            logger.LogError(ex, "Failed to process OrderPaid event for OrderId: {OrderId}, MessageId: {MessageId}",
                @event.OrderId, messageId);
            throw;
        }

        if (emailEnabled)
        {
            try
            {
                var userInfoResult = await identityClient.GetUserInfoAsync(userId, cancellationToken);
                var userInfo = userInfoResult.IsSuccess ? userInfoResult.Value : null;

                if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email) && userInfo.IsActive)
                {
                    var emailSubject = NotificationMessages.OrderPaid.Title;
                    var fullName = !string.IsNullOrWhiteSpace(userInfo.FullName)
                        ? userInfo.FullName
                        : userInfo.Email.Split('@')[0];

                    var emailModel = new
                    {
                        FullName = fullName,
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
                    logger.LogWarning("User info not found, inactive, or email not available for UserId: {UserId}, skipping email notification", userId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email notification to UserId: {UserId} for OrderId: {OrderId}",
                    userId, @event.OrderId);
            }
        }

        logger.LogInformation("OrderPaid notification created for UserId: {UserId}, OrderId: {OrderId}", userId, @event.OrderId);
    }
}
