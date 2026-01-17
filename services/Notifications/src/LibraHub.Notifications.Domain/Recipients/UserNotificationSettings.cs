namespace LibraHub.Notifications.Domain.Recipients;

public class UserNotificationSettings
{
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsStaff { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserNotificationSettings()
    {
    }

    public UserNotificationSettings(
        Guid userId,
        string email,
        bool isActive,
        bool isStaff,
        bool emailEnabled = false,
        bool inAppEnabled = true,
        DateTime? updatedAt = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        UserId = userId;
        Email = email;
        IsActive = isActive;
        IsStaff = isStaff;
        EmailEnabled = emailEnabled;
        InAppEnabled = inAppEnabled;
        UpdatedAt = updatedAt ?? DateTime.UtcNow;
    }

    public void Update(bool emailEnabled, bool inAppEnabled = true)
    {
        EmailEnabled = emailEnabled;
        InAppEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(string email, bool isActive, bool isStaff)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        Email = email;
        IsActive = isActive;
        IsStaff = isStaff;
        UpdatedAt = DateTime.UtcNow;
    }
}
