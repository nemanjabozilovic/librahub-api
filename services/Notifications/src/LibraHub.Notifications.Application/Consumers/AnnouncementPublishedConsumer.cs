using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class AnnouncementPublishedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
    IIdentityClient identityClient,
    IInboxRepository inboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<AnnouncementPublishedConsumer> logger)
{
    private const string EventType = EventTypes.AnnouncementPublishedV1;

    public async Task HandleAsync(AnnouncementPublishedV1 @event, CancellationToken cancellationToken)
    {
        var messageId = $"AnnouncementPublished_{@event.AnnouncementId}";

        logger.LogInformation("Processing AnnouncementPublished event for AnnouncementId: {AnnouncementId}, BookId: {BookId}, Title: {Title}, MessageId: {MessageId}",
            @event.AnnouncementId, @event.BookId, @event.Title, messageId);

        if (await inboxRepository.IsProcessedAsync(messageId, cancellationToken))
        {
            logger.LogInformation("AnnouncementPublished event already processed for MessageId: {MessageId}, AnnouncementId: {AnnouncementId}",
                messageId, @event.AnnouncementId);
            return;
        }

        var allUserIds = await preferencesRepository.GetUserIdsWithEnabledNotificationsAsync(
            NotificationType.AnnouncementPublished,
            cancellationToken);

        logger.LogInformation("Found {Count} users with AnnouncementPublished notifications enabled for AnnouncementId: {AnnouncementId}",
            allUserIds.Count, @event.AnnouncementId);

        var notificationsToCreate = new List<Notification>();

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var userId in allUserIds)
                {
                    try
                    {
                        var preference = await preferencesRepository.GetByUserIdAndTypeAsync(
                            userId,
                            NotificationType.AnnouncementPublished,
                            ct);

                        var emailEnabled = preference?.EmailEnabled ?? true;
                        var inAppEnabled = preference?.InAppEnabled ?? true;

                        if (inAppEnabled)
                        {
                            var notification = new Notification(
                                Guid.NewGuid(),
                                userId,
                                NotificationType.AnnouncementPublished,
                                NotificationMessages.AnnouncementPublished.Title,
                                NotificationMessages.AnnouncementPublished.GetMessage(@event.Title));

                            notificationsToCreate.Add(notification);
                        }

                        if (emailEnabled)
                        {
                            var userInfo = await identityClient.GetUserInfoAsync(userId, ct);

                            if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email) && userInfo.IsActive)
                            {
                                var emailSubject = NotificationMessages.AnnouncementPublished.Title;
                                var fullName = !string.IsNullOrWhiteSpace(userInfo.FullName)
                                    ? userInfo.FullName
                                    : userInfo.Email.Split('@')[0];

                                var emailModel = new
                                {
                                    FullName = fullName,
                                    AnnouncementTitle = @event.Title,
                                    BookId = @event.BookId,
                                    AnnouncementId = @event.AnnouncementId,
                                    PublishedAt = @event.PublishedAt
                                };

                                await SendEmailNotificationAsync(
                                    notificationSender,
                                    userInfo.Email,
                                    emailSubject,
                                    emailModel,
                                    userId,
                                    @event.AnnouncementId,
                                    ct);
                            }
                            else
                            {
                                logger.LogWarning("User info not found, inactive, or email not available for UserId: {UserId}, skipping email notification", userId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process AnnouncementPublished notification for UserId: {UserId} for AnnouncementId: {AnnouncementId}",
                            userId, @event.AnnouncementId);
                    }
                }

                if (notificationsToCreate.Count > 0)
                {
                    await notificationRepository.AddRangeAsync(notificationsToCreate, ct);
                }

                await inboxRepository.MarkAsProcessedAsync(messageId, EventType, ct);
            }, cancellationToken);

            logger.LogInformation("AnnouncementPublished event processed successfully for AnnouncementId: {AnnouncementId}, created {Count} notifications",
                @event.AnnouncementId, notificationsToCreate.Count);

            foreach (var notification in notificationsToCreate)
            {
                try
                {
                    await notificationSender.SendInAppAsync(notification.UserId, notification.Title, notification.Message, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send in-app notification to UserId: {UserId} for AnnouncementId: {AnnouncementId}",
                        notification.UserId, @event.AnnouncementId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process AnnouncementPublished event for AnnouncementId: {AnnouncementId}, MessageId: {MessageId}",
                @event.AnnouncementId, messageId);
            throw;
        }
    }

    private static async Task SendEmailNotificationAsync(
        INotificationSender notificationSender,
        string email,
        string subject,
        object model,
        Guid userId,
        Guid announcementId,
        CancellationToken cancellationToken)
    {
        try
        {
            await notificationSender.SendEmailWithTemplateAsync(
                email,
                subject,
                "ANNOUNCEMENT_PUBLISHED",
                model,
                cancellationToken);
        }
        catch (Exception)
        {
            // Logging will be handled by the caller if needed
            throw;
        }
    }
}
