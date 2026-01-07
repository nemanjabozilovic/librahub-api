using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Constants;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class BookPublishedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
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

        var allUserIds = await preferencesRepository.GetUserIdsWithEnabledNotificationsAsync(
            NotificationType.BookPublished,
            cancellationToken);

        logger.LogInformation("Found {Count} users with BookPublished notifications enabled for BookId: {BookId}",
            allUserIds.Count, @event.BookId);

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
                            NotificationType.BookPublished,
                            ct);

                        var emailEnabled = preference?.EmailEnabled ?? true;
                        var inAppEnabled = preference?.InAppEnabled ?? true;

                        if (inAppEnabled)
                        {
                            var notification = new Notification(
                                Guid.NewGuid(),
                                userId,
                                NotificationType.BookPublished,
                                NotificationMessages.BookPublished.Title,
                                NotificationMessages.BookPublished.GetMessage(@event.Title, @event.Authors));

                            notificationsToCreate.Add(notification);
                        }

                        if (emailEnabled)
                        {
                            var userInfo = await identityClient.GetUserInfoAsync(userId, ct);

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

                                await SendEmailNotificationAsync(
                                    notificationSender,
                                    userInfo.Email,
                                    emailSubject,
                                    emailModel,
                                    userId,
                                    @event.BookId,
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
                        logger.LogError(ex, "Failed to process BookPublished notification for UserId: {UserId} for BookId: {BookId}",
                            userId, @event.BookId);
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

    private static async Task SendEmailNotificationAsync(
        INotificationSender notificationSender,
        string email,
        string subject,
        object model,
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken)
    {
        try
        {
            await notificationSender.SendEmailWithTemplateAsync(
                email,
                subject,
                "BOOK_PUBLISHED",
                model,
                cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
