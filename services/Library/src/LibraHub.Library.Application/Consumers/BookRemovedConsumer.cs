using LibraHub.Contracts.Catalog.V1;
using LibraHub.Library.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Library.Application.Consumers;

public class BookRemovedConsumer(
    IBookSnapshotStore bookSnapshotStore,
    ILogger<BookRemovedConsumer> logger)
{
    public async Task HandleAsync(BookRemovedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing BookRemoved event for BookId: {BookId}", @event.BookId);

        var snapshot = await bookSnapshotStore.GetByIdAsync(@event.BookId, cancellationToken);

        if (snapshot == null)
        {
            logger.LogWarning("Book snapshot not found for BookId: {BookId}", @event.BookId);
            return;
        }

        snapshot.MarkAsRemoved();
        await bookSnapshotStore.AddOrUpdateAsync(snapshot, cancellationToken);

        logger.LogInformation("Marked book snapshot as removed for BookId: {BookId}", @event.BookId);
    }
}
