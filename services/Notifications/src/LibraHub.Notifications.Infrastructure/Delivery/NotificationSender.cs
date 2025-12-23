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
            // Send notification to user's SignalR group
            await notificationHub.SendToUserAsync(
                userId,
                "ReceiveNotification",
                new { Title = title, Message = message },
                cancellationToken);

            logger.LogInformation("Notification sent via SignalR to UserId: {UserId}, Title: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification via SignalR to UserId: {UserId}", userId);
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

