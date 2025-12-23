using LibraHub.Contracts.Catalog.V1;
using LibraHub.Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Notifications.Application.Consumers;

public class BookPublishedConsumer(
    INotificationRepository notificationRepository,
    INotificationPreferencesRepository preferencesRepository,
    INotificationSender notificationSender,
    ILogger<BookPublishedConsumer> logger)
{
    public async Task HandleAsync(BookPublishedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing BookPublished event for BookId: {BookId}", @event.BookId);

        // Get all users who have BookPublished notifications enabled
        // For now, create notifications for all users with preferences enabled
        // In a real scenario, it might need to query users who are interested in new books
        var preferences = await preferencesRepository.GetByUserIdAsync(Guid.Empty, cancellationToken);

        // For simplicity, skip this consumer for now and handle it differently
        // This would require a user subscription/preference system

        logger.LogInformation("BookPublished event processed for BookId: {BookId}", @event.BookId);
    }
}

