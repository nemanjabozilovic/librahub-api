using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Recipients;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class UserNotificationSettingsChangedConsumer(
    IUserNotificationSettingsRepository settingsRepository,
    IClock clock,
    ILogger<UserNotificationSettingsChangedConsumer> logger)
{
    public async Task HandleAsync(UserNotificationSettingsChangedV1 @event, CancellationToken cancellationToken)
    {
        var emailEnabled = @event.EmailAnnouncementsEnabled || @event.EmailPromotionsEnabled;

        var settings = new UserNotificationSettings(
            @event.UserId,
            @event.Email ?? string.Empty,
            @event.IsActive,
            @event.IsStaff,
            emailEnabled: emailEnabled,
            inAppEnabled: true,
            updatedAt: clock.UtcNow);

        await settingsRepository.UpsertAsync(settings, cancellationToken);

        logger.LogInformation(
            "Upserted notification settings for UserId: {UserId}, IsStaff: {IsStaff}, EmailEnabled: {EmailEnabled}, InAppEnabled: {InAppEnabled}",
            @event.UserId,
            @event.IsStaff,
            emailEnabled,
            true);
    }
}

