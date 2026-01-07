using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Commands.UnlistBook;

public class UnlistBookHandler(
    IBookRepository bookRepository,
    IOutboxWriter outboxWriter,
    ICache cache) : IRequestHandler<UnlistBookCommand, Result>
{
    public async Task<Result> Handle(UnlistBookCommand request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        try
        {
            book.Unlist();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await bookRepository.UpdateAsync(book, cancellationToken);

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookUnlistedV1
            {
                BookId = book.Id,
                Title = book.Title,
                UnlistedAt = new DateTimeOffset(book.UpdatedAt, TimeSpan.Zero)
            },
            Contracts.Common.EventTypes.BookUnlisted,
            cancellationToken);

        await CacheInvalidationHelper.InvalidateBookCacheAsync(cache, book.Id, cancellationToken);

        return Result.Success();
    }
}
