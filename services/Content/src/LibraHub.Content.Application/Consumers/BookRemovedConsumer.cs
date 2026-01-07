using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Contracts.Catalog.V1;
using Microsoft.Extensions.Logging;

namespace LibraHub.Content.Application.Consumers;

public class BookRemovedConsumer(
    IStoredObjectRepository storedObjectRepository,
    IBookEditionRepository editionRepository,
    ICoverRepository coverRepository,
    IUnitOfWork unitOfWork,
    ILogger<BookRemovedConsumer> logger)
{
    public async Task HandleAsync(BookRemovedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing BookRemoved event for BookId: {BookId}, Reason: {Reason}", @event.BookId, @event.Reason);

        var blockReason = $"Book removed: {@event.Reason}";

        var storedObjects = await storedObjectRepository.GetByBookIdAsync(@event.BookId, cancellationToken);
        var editions = await editionRepository.GetByBookIdAsync(@event.BookId, cancellationToken);
        var cover = await coverRepository.GetByBookIdAsync(@event.BookId, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(ct =>
        {
            foreach (var obj in storedObjects)
            {
                obj.Block(blockReason);
            }

            foreach (var edition in editions)
            {
                edition.Block();
            }

            if (cover != null)
            {
                cover.Block();
            }

            return Task.CompletedTask;
        }, cancellationToken);

        logger.LogInformation("All content blocked for BookId: {BookId}", @event.BookId);
    }
}
