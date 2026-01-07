using LibraHub.Contracts.Catalog.V1;
using LibraHub.Library.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Library.Application.Consumers;

public class BookUpdatedConsumer(
    IBookSnapshotStore bookSnapshotStore,
    ILogger<BookUpdatedConsumer> logger)
{
    public async Task HandleAsync(BookUpdatedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing BookUpdated event for BookId: {BookId}", @event.BookId);

        var snapshot = await bookSnapshotStore.GetByIdAsync(@event.BookId, cancellationToken);

        if (snapshot == null)
        {
            logger.LogWarning("Book snapshot not found for BookId: {BookId}, creating new snapshot", @event.BookId);

            var newSnapshot = new Domain.Books.BookSnapshot(
                @event.BookId,
                @event.Title,
                @event.Authors);

            await bookSnapshotStore.AddOrUpdateAsync(newSnapshot, cancellationToken);
        }
        else
        {
            snapshot.Update(@event.Title, @event.Authors);
            await bookSnapshotStore.AddOrUpdateAsync(snapshot, cancellationToken);
        }

        logger.LogInformation("Updated book snapshot for BookId: {BookId}", @event.BookId);
    }
}
