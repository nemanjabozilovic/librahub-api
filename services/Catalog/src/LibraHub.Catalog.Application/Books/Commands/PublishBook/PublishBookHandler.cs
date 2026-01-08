using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Commands.PublishBook;

public class PublishBookHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository,
    IBookContentStateRepository contentStateRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork,
    ICache cache) : IRequestHandler<PublishBookCommand, Result>
{
    public async Task<Result> Handle(PublishBookCommand request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        var pricing = await pricingRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        if (pricing == null)
        {
            return Result.Failure(Error.Validation(CatalogErrors.Pricing.NotFound));
        }

        var contentState = await contentStateRepository.GetByBookIdAsync(request.BookId, cancellationToken);

        try
        {
            book.Publish(pricing, contentState);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not ready for publishing"))
            {
                return Result.Failure(Error.Validation(
                    "Book content is not ready yet. The cover or edition may still be processing. Please wait a few seconds and try again."));
            }
            return Result.Failure(Error.Validation(ex.Message));
        }

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await bookRepository.UpdateAsync(book, ct);

                var authors = string.Join(", ", book.Authors.Select(a => a.Name));

                await outboxWriter.WriteAsync(
                    new Contracts.Catalog.V1.BookPublishedV1
                    {
                        BookId = book.Id,
                        Title = book.Title,
                        Authors = authors,
                        PublishedAt = new DateTimeOffset(book.UpdatedAt, TimeSpan.Zero)
                    },
                    Contracts.Common.EventTypes.BookPublished,
                    ct);
            }, cancellationToken);

            await CacheInvalidationHelper.InvalidateBookCacheAsync(cache, book.Id, cancellationToken);

            return Result.Success();
        }
        catch
        {
            throw;
        }
    }
}
