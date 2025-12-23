using LibraHub.Contracts.Library.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class EntitlementGrantedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
    IIdentityClient identityClient,
    ILogger<EntitlementGrantedConsumer> logger)
{
    public async Task HandleAsync(EntitlementGrantedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing EntitlementGranted event for UserId: {UserId}, BookId: {BookId}",
            @event.UserId, @event.BookId);

        var userId = @event.UserId;

        // Check user preferences
        var preference = await preferencesRepository.GetByUserIdAndTypeAsync(
            userId,
            NotificationType.EntitlementGranted,
            cancellationToken);

        var emailEnabled = preference?.EmailEnabled ?? true;
        var inAppEnabled = preference?.InAppEnabled ?? true;

        if (inAppEnabled)
        {
            // Create in-app notification
            var notification = new Notification(
                Guid.NewGuid(),
                userId,
                NotificationType.EntitlementGranted,
                "Book added to your library",
                $"A new book has been added to your library. Book ID: {@event.BookId}");

            await notificationRepository.AddAsync(notification, cancellationToken);
            await notificationSender.SendInAppAsync(userId, notification.Title, notification.Message, cancellationToken);
        }

        if (emailEnabled)
        {
            // Get user info from Identity service
            var userInfo = await identityClient.GetUserInfoAsync(userId, cancellationToken);

            if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email))
            {
                var emailSubject = "Book Added to Your Library";
                var emailModel = new
                {
                    FullName = !string.IsNullOrWhiteSpace(userInfo.FullName) ? userInfo.FullName : $"User {userId}",
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
                logger.LogWarning("User info not found or email not available for UserId: {UserId}, skipping email notification", userId);
            }
        }

        logger.LogInformation("EntitlementGranted notification created for UserId: {UserId}, BookId: {BookId}", userId, @event.BookId);
    }
}

