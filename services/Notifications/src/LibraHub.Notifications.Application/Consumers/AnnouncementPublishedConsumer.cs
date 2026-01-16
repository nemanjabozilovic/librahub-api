using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class AnnouncementPublishedConsumer(
    INotificationRepository notificationRepository,
    INotificationSender notificationSender,
    IUserNotificationSettingsRepository settingsRepository,
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

        var eligibleInAppUserIds = await settingsRepository.GetActiveNonStaffUserIdsWithInAppEnabledAsync(cancellationToken);
        var emailRecipients = await settingsRepository.GetEmailRecipientsAsync(cancellationToken);

        logger.LogInformation(
            "AnnouncementPublished recipients for AnnouncementId: {AnnouncementId}, InApp: {InAppCount}, Email: {EmailCount}",
            @event.AnnouncementId,
            eligibleInAppUserIds.Count,
            emailRecipients.Count);

        var notificationsToCreate = new List<Notification>(eligibleInAppUserIds.Count);
        foreach (var userId in eligibleInAppUserIds)
        {
            notificationsToCreate.Add(new Notification(
                Guid.NewGuid(),
                userId,
                NotificationType.AnnouncementPublished,
                @event.Title,
                @event.Content,
                @event.ImageUrl));
        }

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
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
                    await notificationSender.SendAnnouncementPublishedAsync(
                        notification.UserId,
                        @event.AnnouncementId,
                        @event.BookId,
                        @event.Title,
                        @event.Content,
                        @event.ImageUrl,
                        @event.PublishedAt,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send in-app notification to UserId: {UserId} for AnnouncementId: {AnnouncementId}",
                        notification.UserId, @event.AnnouncementId);
                }
            }

            foreach (var recipient in emailRecipients)
            {
                try
                {
                    var emailSubject = NotificationMessages.AnnouncementPublished.Title;
                    var email = recipient.Email;
                    var fullName = email.Split('@')[0];
                    var emailModel = new
                    {
                        FullName = fullName,
                        AnnouncementTitle = @event.Title,
                        AnnouncementContent = @event.Content,
                        BookId = @event.BookId,
                        AnnouncementId = @event.AnnouncementId,
                        ImageUrl = @event.ImageUrl,
                        PublishedAt = @event.PublishedAt
                    };

                    await notificationSender.SendEmailWithTemplateAsync(
                        email,
                        emailSubject,
                        "ANNOUNCEMENT_PUBLISHED",
                        emailModel,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send announcement email to UserId: {UserId} for AnnouncementId: {AnnouncementId}",
                        recipient.UserId, @event.AnnouncementId);
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
}
