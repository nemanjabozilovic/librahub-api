using LibraHub.Contracts.Catalog.V1;
using LibraHub.Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class AnnouncementPublishedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
    ILogger<AnnouncementPublishedConsumer> logger)
{
    public async Task HandleAsync(AnnouncementPublishedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing AnnouncementPublished event for AnnouncementId: {AnnouncementId}, BookId: {BookId}",
            @event.AnnouncementId, @event.BookId);

        // This would require user subscription system
        // For now, we'll log and skip

        logger.LogInformation("AnnouncementPublished event processed for AnnouncementId: {AnnouncementId}", @event.AnnouncementId);
    }
}

