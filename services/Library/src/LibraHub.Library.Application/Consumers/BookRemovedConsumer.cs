using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Library.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Library.Application.Consumers;

public class BookRemovedConsumer(
    IBookSnapshotStore bookSnapshotStore,
    IEntitlementRepository entitlementRepository,
    IReadingProgressRepository readingProgressRepository,
    IUnitOfWork unitOfWork,
    ILogger<BookRemovedConsumer> logger)
{
    public async Task HandleAsync(BookRemovedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing BookRemoved event for BookId: {BookId}, Reason: {Reason}", @event.BookId, @event.Reason);

        var snapshot = await bookSnapshotStore.GetByIdAsync(@event.BookId, cancellationToken);

        if (snapshot == null)
        {
            logger.LogWarning("Book snapshot not found for BookId: {BookId}", @event.BookId);
        }
        else
        {
            snapshot.MarkAsRemoved();
            await bookSnapshotStore.AddOrUpdateAsync(snapshot, cancellationToken);
            logger.LogInformation("Marked book snapshot as removed for BookId: {BookId}", @event.BookId);
        }

        var entitlements = await entitlementRepository.GetByBookIdAsync(@event.BookId, cancellationToken);

        if (entitlements.Count > 0)
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var entitlement in entitlements)
                {
                    entitlement.Revoke($"Book removed: {@event.Reason}");
                    await entitlementRepository.UpdateAsync(entitlement, ct);
                }
            }, cancellationToken);

            logger.LogInformation("Revoked {Count} entitlements for BookId: {BookId}", entitlements.Count, @event.BookId);
        }

        var readingProgress = await readingProgressRepository.GetByBookIdAsync(@event.BookId, cancellationToken);

        if (readingProgress.Count > 0)
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var progress in readingProgress)
                {
                    await readingProgressRepository.DeleteAsync(progress, ct);
                }
            }, cancellationToken);

            logger.LogInformation("Deleted {Count} reading progress records for BookId: {BookId}", readingProgress.Count, @event.BookId);
        }
    }
}
