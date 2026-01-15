using LibraHub.Notifications.Domain.Notifications;

namespace LibraHub.Notifications.Domain.Preferences;

public class NotificationPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NotificationPreference()
    {
    }

    public NotificationPreference(
        Guid id,
        Guid userId,
        NotificationType type,
        bool emailEnabled = false,
        bool inAppEnabled = false)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        Id = id;
        UserId = userId;
        Type = type;
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(bool emailEnabled, bool inAppEnabled)
    {
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
