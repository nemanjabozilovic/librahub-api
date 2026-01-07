using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Commands.RemoveBook;

public class RemoveBookHandler(
    IBookRepository bookRepository,
    ICurrentUser currentUser,
    IOutboxWriter outboxWriter,
    ICache cache) : IRequestHandler<RemoveBookCommand, Result>
{
    public async Task<Result> Handle(RemoveBookCommand request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        if (!currentUser.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized("User not authenticated"));
        }

        try
        {
            book.Remove(currentUser.UserId.Value, request.Reason);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await bookRepository.UpdateAsync(book, cancellationToken);

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookRemovedV1
            {
                BookId = book.Id,
                Title = book.Title,
                RemovedBy = currentUser.UserId.Value,
                Reason = request.Reason,
                RemovedAt = new DateTimeOffset(book.RemovedAt!.Value, TimeSpan.Zero)
            },
            Contracts.Common.EventTypes.BookRemoved,
            cancellationToken);

        await CacheInvalidationHelper.InvalidateBookCacheAsync(cache, book.Id, cancellationToken);

        return Result.Success();
    }
}
