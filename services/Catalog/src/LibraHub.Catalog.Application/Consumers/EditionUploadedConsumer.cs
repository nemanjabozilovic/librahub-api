using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Contracts.Content.V1;
using Microsoft.Extensions.Logging;

namespace LibraHub.Catalog.Application.Consumers;

public class EditionUploadedConsumer(
    IBookContentStateRepository contentStateRepository,
    ILogger<EditionUploadedConsumer> logger)
{
    public async Task HandleAsync(EditionUploadedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing EditionUploaded event for BookId: {BookId}, Format: {Format}", @event.BookId, @event.Format);

        var contentState = await contentStateRepository.GetByBookIdAsync(@event.BookId, cancellationToken);

        if (contentState == null)
        {
            contentState = new Domain.Projections.BookContentState(@event.BookId);
            await contentStateRepository.AddAsync(contentState, cancellationToken);
        }

        contentState.SetEdition();
        await contentStateRepository.UpdateAsync(contentState, cancellationToken);

        logger.LogInformation("Edition state updated for BookId: {BookId}", @event.BookId);
    }
}
