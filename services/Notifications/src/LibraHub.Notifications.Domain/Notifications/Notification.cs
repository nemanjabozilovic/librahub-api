namespace LibraHub.Notifications.Domain.Notifications;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification()
    {
    }

    public Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string message)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        Id = id;
        UserId = userId;
        Type = type;
        Title = title;
        Message = message;
        Status = NotificationStatus.Unread;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        if (Status == NotificationStatus.Read)
        {
            return;
        }

        Status = NotificationStatus.Read;
        ReadAt = DateTime.UtcNow;
    }
}
