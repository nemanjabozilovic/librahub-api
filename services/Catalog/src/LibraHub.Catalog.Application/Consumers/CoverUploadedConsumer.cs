using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Contracts.Content.V1;
using Microsoft.Extensions.Logging;

namespace LibraHub.Catalog.Application.Consumers;

public class CoverUploadedConsumer(
    IBookContentStateRepository contentStateRepository,
    ILogger<CoverUploadedConsumer> logger)
{
    public async Task HandleAsync(CoverUploadedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing CoverUploaded event for BookId: {BookId}", @event.BookId);

        var contentState = await contentStateRepository.GetByBookIdAsync(@event.BookId, cancellationToken);

        if (contentState == null)
        {
            contentState = new Domain.Projections.BookContentState(@event.BookId);
            await contentStateRepository.AddAsync(contentState, cancellationToken);
        }

        contentState.SetCover(@event.CoverRef);
        await contentStateRepository.UpdateAsync(contentState, cancellationToken);

        logger.LogInformation("Cover state updated for BookId: {BookId}", @event.BookId);
    }
}
