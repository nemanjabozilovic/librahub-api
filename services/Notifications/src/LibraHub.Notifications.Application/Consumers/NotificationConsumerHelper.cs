using LibraHub.Notifications.Domain.Recipients;

namespace LibraHub.Notifications.Application.Consumers;

public static class NotificationConsumerHelper
{
    public static bool ShouldReceiveNotifications(UserNotificationSettings? userSettings)
    {
        if (userSettings == null)
            return false;

        if (userSettings.IsStaff)
            return false;

        return userSettings.IsActive;
    }
}
