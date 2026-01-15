namespace LibraHub.Notifications.Domain.Recipients;

public class UserNotificationSettings
{
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsStaff { get; private set; }
    public bool EmailAnnouncementsEnabled { get; private set; }
    public bool EmailPromotionsEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserNotificationSettings()
    {
    }

    public UserNotificationSettings(
        Guid userId,
        string email,
        bool isActive,
        bool isStaff,
        bool emailAnnouncementsEnabled,
        bool emailPromotionsEnabled,
        DateTime updatedAt)
    {
        UserId = userId;
        Email = email;
        IsActive = isActive;
        IsStaff = isStaff;
        EmailAnnouncementsEnabled = emailAnnouncementsEnabled;
        EmailPromotionsEnabled = emailPromotionsEnabled;
        UpdatedAt = updatedAt;
    }
}

