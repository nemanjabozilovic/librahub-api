using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Recipients;
using Microsoft.Extensions.Logging;

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

    public static async Task SendTemplatedEmailIfActiveAsync(
        IIdentityClient identityClient,
        INotificationSender notificationSender,
        ILogger logger,
        Guid userId,
        string subject,
        string templateName,
        Func<string, object> buildModel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userInfoResult = await identityClient.GetUserInfoAsync(userId, cancellationToken);
            var userInfo = userInfoResult.IsSuccess ? userInfoResult.Value : null;

            if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.Email) && userInfo.IsActive)
            {
                var fullName = !string.IsNullOrWhiteSpace(userInfo.FullName)
                    ? userInfo.FullName
                    : userInfo.Email.Split('@')[0];

                await notificationSender.SendEmailWithTemplateAsync(
                    userInfo.Email,
                    subject,
                    templateName,
                    buildModel(fullName),
                    cancellationToken);
            }
            else
            {
                logger.LogWarning("User info not found, inactive, or email not available for UserId: {UserId}, skipping email notification", userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email notification to UserId: {UserId}", userId);
        }
    }
}
