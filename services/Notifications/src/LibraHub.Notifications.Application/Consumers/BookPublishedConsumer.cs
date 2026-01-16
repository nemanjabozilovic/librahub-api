using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class BookPublishedConsumer(
    INotificationRepository notificationRepository,
    INotificationSender notificationSender,
    IUserNotificationSettingsRepository settingsRepository,
    IIdentityClient identityClient,
    IInboxRepository inboxRepository,
    IUnitOfWork unitOfWork,
    ILogger<BookPublishedConsumer> logger)
{
    private const string EventType = EventTypes.BookPublishedV1;

    public async Task HandleAsync(BookPublishedV1 @event, CancellationToken cancellationToken)
    {
        var messageId = $"BookPublished_{@event.BookId}";

        logger.LogInformation("Processing BookPublished event for BookId: {BookId}, Title: {Title}, MessageId: {MessageId}",
            @event.BookId, @event.Title, messageId);

        if (await inboxRepository.IsProcessedAsync(messageId, cancellationToken))
        {
            logger.LogInformation("BookPublished event already processed for MessageId: {MessageId}, BookId: {BookId}",
                messageId, @event.BookId);
            return;
        }

        var eligibleInAppUserIds = await settingsRepository.GetActiveNonStaffUserIdsWithInAppEnabledAsync(cancellationToken);
        var emailRecipients = await settingsRepository.GetEmailRecipientsAsync(cancellationToken);

        logger.LogInformation("Found {InAppCount} users with in-app and {EmailCount} users with email notifications enabled for BookId: {BookId}",
            eligibleInAppUserIds.Count, emailRecipients.Count, @event.BookId);

        var notificationsToCreate = new List<Notification>();

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var userId in eligibleInAppUserIds)
                {
                    var notification = new Notification(
                        Guid.NewGuid(),
                        userId,
                        NotificationType.BookPublished,
                        NotificationMessages.BookPublished.Title,
                        NotificationMessages.BookPublished.GetMessage(@event.Title, @event.Authors));

                    notificationsToCreate.Add(notification);
                }

                foreach (var recipient in emailRecipients)
                {
                    try
                    {
                        var userInfoResult = await identityClient.GetUserInfoAsync(recipient.UserId, ct);
                        var userInfo = userInfoResult.IsSuccess ? userInfoResult.Value : null;

                        if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email) && userInfo.IsActive)
                        {
                            var emailSubject = NotificationMessages.BookPublished.Title;
                            var fullName = !string.IsNullOrWhiteSpace(userInfo.FullName)
                                ? userInfo.FullName
                                : userInfo.Email.Split('@')[0];

                            var emailModel = new
                            {
                                FullName = fullName,
                                BookTitle = @event.Title,
                                Authors = @event.Authors,
                                BookId = @event.BookId,
                                PublishedAt = @event.PublishedAt
                            };

                            await notificationSender.SendEmailWithTemplateAsync(
                                userInfo.Email,
                                emailSubject,
                                "BOOK_PUBLISHED",
                                emailModel,
                                ct);
                        }
                        else
                        {
                            logger.LogWarning("User info not found, inactive, or email not available for UserId: {UserId}, skipping email notification", recipient.UserId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send email notification for UserId: {UserId} for BookId: {BookId}",
                            recipient.UserId, @event.BookId);
                    }
                }

                if (notificationsToCreate.Count > 0)
                {
                    await notificationRepository.AddRangeAsync(notificationsToCreate, ct);
                }

                await inboxRepository.MarkAsProcessedAsync(messageId, EventType, ct);
            }, cancellationToken);

            logger.LogInformation("BookPublished event processed successfully for BookId: {BookId}, created {Count} notifications",
                @event.BookId, notificationsToCreate.Count);

            foreach (var notification in notificationsToCreate)
            {
                try
                {
                    await notificationSender.SendInAppAsync(notification.UserId, notification.Title, notification.Message, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send in-app notification to UserId: {UserId} for BookId: {BookId}",
                        notification.UserId, @event.BookId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process BookPublished event for BookId: {BookId}, MessageId: {MessageId}",
                @event.BookId, messageId);
            throw;
        }
    }
}
