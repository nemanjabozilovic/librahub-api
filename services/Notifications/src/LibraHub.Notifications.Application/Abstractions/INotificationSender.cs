namespace LibraHub.Notifications.Application.Abstractions;

public interface INotificationSender
{
    Task SendInAppAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);

    Task SendAnnouncementPublishedAsync(
        Guid userId,
        Guid announcementId,
        Guid? bookId,
        string title,
        string content,
        string? imageUrl,
        DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default);

    Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);

    Task SendEmailWithTemplateAsync(string email, string subject, string templateName, object model, CancellationToken cancellationToken = default);
}
