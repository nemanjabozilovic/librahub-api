using LibraHub.BuildingBlocks.Caching;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books;
using LibraHub.Contracts.Content.V1;
using Microsoft.Extensions.Logging;

namespace LibraHub.Catalog.Application.Consumers;

public class CoverUploadedConsumer(
    IBookContentStateRepository contentStateRepository,
    ICache cache,
    ILogger<CoverUploadedConsumer> logger)
{
    public async Task HandleAsync(CoverUploadedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing CoverUploaded event for BookId: {BookId}, CoverRef: {CoverRef}", @event.BookId, @event.CoverRef);

        var contentState = await contentStateRepository.GetByBookIdAsync(@event.BookId, cancellationToken);

        if (contentState == null)
        {
            logger.LogInformation("Creating new BookContentState for BookId: {BookId}", @event.BookId);
            contentState = new Domain.Projections.BookContentState(@event.BookId);
            await contentStateRepository.AddAsync(contentState, cancellationToken);
        }
        else
        {
            logger.LogInformation("Updating existing BookContentState for BookId: {BookId}, Current CoverRef: {CurrentCoverRef}", @event.BookId, contentState.CoverRef);
        }

        contentState.SetCover(@event.CoverRef);
        await contentStateRepository.UpdateAsync(contentState, cancellationToken);

        var updatedState = await contentStateRepository.GetByBookIdAsync(@event.BookId, cancellationToken);
        logger.LogInformation("BookContentState after update for BookId: {BookId}, CoverRef: {CoverRef}, HasCover: {HasCover}",
            @event.BookId, updatedState?.CoverRef, updatedState?.HasCover);

        await CacheInvalidationHelper.InvalidateBookCacheAsync(cache, @event.BookId, cancellationToken);

        logger.LogInformation("Cover state updated and cache invalidated for BookId: {BookId}", @event.BookId);
    }
}
