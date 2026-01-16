using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Infrastructure.Delivery;

public class NotificationSender(
    INotificationHub notificationHub,
    IEmailSender emailSender,
    ILogger<NotificationSender> logger) : INotificationSender
{
    public async Task SendInAppAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Sending SignalR notification via method 'ReceiveNotification' to UserId: {UserId}, Title: {Title}", userId, title);

            await notificationHub.SendToUserAsync(
                userId,
                "ReceiveNotification",
                new { Title = title, Message = message },
                cancellationToken);

            logger.LogInformation("Notification sent successfully via SignalR to UserId: {UserId}, Title: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification via SignalR to UserId: {UserId}, Method: ReceiveNotification", userId);
        }
    }

    public async Task SendAnnouncementPublishedAsync(
        Guid userId,
        Guid announcementId,
        Guid? bookId,
        string title,
        string content,
        string? imageUrl,
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Sending SignalR announcement via method 'ReceiveAnnouncement' to UserId: {UserId}, AnnouncementId: {AnnouncementId}, BookId: {BookId}, Title: {Title}",
                userId, announcementId, bookId, title);

            await notificationHub.SendToUserAsync(
                userId,
                "ReceiveAnnouncement",
                new
                {
                    AnnouncementId = announcementId,
                    BookId = bookId,
                    Title = title,
                    Content = content,
                    ImageUrl = imageUrl,
                    PublishedAt = publishedAt
                },
                cancellationToken);

            logger.LogInformation(
                "Announcement notification sent successfully via SignalR to UserId: {UserId}, AnnouncementId: {AnnouncementId}, Method: ReceiveAnnouncement",
                userId, announcementId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send announcement notification via SignalR to UserId: {UserId}, AnnouncementId: {AnnouncementId}, Method: ReceiveAnnouncement",
                userId, announcementId);
        }
    }

    public Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        return emailSender.SendEmailAsync(email, subject, body, cancellationToken);
    }

    public Task SendEmailWithTemplateAsync(string email, string subject, string templateName, object model, CancellationToken cancellationToken = default)
    {
        return emailSender.SendEmailWithTemplateAsync(email, subject, templateName, model, cancellationToken);
    }
}
