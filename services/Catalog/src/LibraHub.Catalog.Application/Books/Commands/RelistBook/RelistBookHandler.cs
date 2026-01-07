using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Commands.RelistBook;

public class RelistBookHandler(
    IBookRepository bookRepository,
    IOutboxWriter outboxWriter,
    ICache cache) : IRequestHandler<RelistBookCommand, Result>
{
    public async Task<Result> Handle(RelistBookCommand request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        try
        {
            book.Relist();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await bookRepository.UpdateAsync(book, cancellationToken);

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookRelistedV1
            {
                BookId = book.Id,
                Title = book.Title,
                RelistedAt = new DateTimeOffset(book.UpdatedAt, TimeSpan.Zero)
            },
            Contracts.Common.EventTypes.BookRelisted,
            cancellationToken);

        await CacheInvalidationHelper.InvalidateBookCacheAsync(cache, book.Id, cancellationToken);

        return Result.Success();
    }
}
