using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.Contracts.Catalog.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraHub.Content.Application.Consumers;

public class BookRemovedConsumer(
    IStoredObjectRepository storedObjectRepository,
    IBookEditionRepository editionRepository,
    ICoverRepository coverRepository,
    IObjectStorage objectStorage,
    IUnitOfWork unitOfWork,
    IOptions<UploadOptions> uploadOptions,
    ILogger<BookRemovedConsumer> logger)
{
    public async Task HandleAsync(BookRemovedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing BookRemoved event for BookId: {BookId}, Reason: {Reason}", @event.BookId, @event.Reason);

        var storedObjects = await storedObjectRepository.GetByBookIdAsync(@event.BookId, cancellationToken);
        var editions = await editionRepository.GetByBookIdAsync(@event.BookId, cancellationToken);
        var cover = await coverRepository.GetByBookIdAsync(@event.BookId, cancellationToken);

        foreach (var storedObject in storedObjects)
        {
            try
            {
                var bucketName = IsCoverObjectKey(storedObject.ObjectKey)
                    ? uploadOptions.Value.CoversBucketName
                    : uploadOptions.Value.EditionsBucketName;

                await objectStorage.DeleteAsync(bucketName, storedObject.ObjectKey, cancellationToken);
                logger.LogInformation("Deleted object from storage: {ObjectKey} for BookId: {BookId}", storedObject.ObjectKey, @event.BookId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete object from storage: {ObjectKey} for BookId: {BookId}", storedObject.ObjectKey, @event.BookId);
            }
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (cover != null)
            {
                await coverRepository.DeleteAsync(cover, ct);
            }

            foreach (var edition in editions)
            {
                await editionRepository.DeleteAsync(edition, ct);
            }

            foreach (var storedObject in storedObjects)
            {
                await storedObjectRepository.DeleteAsync(storedObject, ct);
            }
        }, cancellationToken);

        logger.LogInformation("All content deleted for BookId: {BookId}", @event.BookId);
    }

    private static bool IsCoverObjectKey(string objectKey)
    {
        return objectKey.Contains("/cover/", StringComparison.OrdinalIgnoreCase);
    }
}
