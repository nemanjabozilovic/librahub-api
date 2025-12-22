using LibraHub.BuildingBlocks.Abstractions;
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
    IOutboxWriter outboxWriter) : IRequestHandler<PublishBookCommand, Result>
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
            return Result.Failure(Error.Validation(ex.Message));
        }

        await bookRepository.UpdateAsync(book, cancellationToken);

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookPublishedV1
            {
                BookId = book.Id,
                Title = book.Title,
                PublishedAt = book.UpdatedAt
            },
            Contracts.Common.EventTypes.BookPublished,
            cancellationToken);

        return Result.Success();
    }
}
