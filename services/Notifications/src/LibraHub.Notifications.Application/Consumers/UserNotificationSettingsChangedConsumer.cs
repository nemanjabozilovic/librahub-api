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
        var settings = new UserNotificationSettings(
            @event.UserId,
            @event.Email ?? string.Empty,
            @event.IsActive,
            @event.IsStaff,
            @event.EmailAnnouncementsEnabled,
            @event.EmailPromotionsEnabled,
            clock.UtcNow);

        await settingsRepository.UpsertAsync(settings, cancellationToken);

        logger.LogInformation(
            "Upserted notification settings for UserId: {UserId}, IsStaff: {IsStaff}, AnnouncementsEmail: {Announcements}, PromotionsEmail: {Promotions}",
            @event.UserId,
            @event.IsStaff,
            @event.EmailAnnouncementsEnabled,
            @event.EmailPromotionsEnabled);
    }
}

