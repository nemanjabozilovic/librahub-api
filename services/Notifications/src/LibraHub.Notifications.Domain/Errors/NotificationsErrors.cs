namespace LibraHub.Notifications.Domain.Errors;

public static class NotificationsErrors
{
    public static class Notification
    {
        public const string NotFound = "NOTIFICATION_NOT_FOUND";
        public const string AlreadyRead = "NOTIFICATION_ALREADY_READ";
    }

    public static class Preference
    {
        public const string NotFound = "PREFERENCE_NOT_FOUND";
    }

    public static class User
    {
        public const string NotAuthenticated = "USER_NOT_AUTHENTICATED";
    }
}

