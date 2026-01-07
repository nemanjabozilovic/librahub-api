using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Books;
using LibraHub.Library.Domain.Errors;
using MediatR;

namespace LibraHub.Library.Application.Entitlements.Queries.MyBooks;

public class MyBooksHandler(
    IEntitlementRepository entitlementRepository,
    IBookSnapshotStore bookSnapshotStore,
    ICurrentUser currentUser) : IRequestHandler<MyBooksQuery, Result<MyBooksDto>>
{
    public async Task<Result<MyBooksDto>> Handle(MyBooksQuery request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(LibraryErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure<MyBooksDto>(userIdResult.Error!);
        }

        var userId = userIdResult.Value;

        var totalCount = await entitlementRepository.CountActiveByUserIdAsync(userId, cancellationToken);

        var pagedEntitlements = await entitlementRepository.GetActiveByUserIdPagedAsync(
            userId,
            request.Skip,
            request.Take,
            cancellationToken);

        var bookIds = pagedEntitlements.Select(e => e.BookId).ToList();
        var snapshotDict = new Dictionary<Guid, BookSnapshot>();

        foreach (var bookId in bookIds)
        {
            var snapshot = await bookSnapshotStore.GetByIdAsync(bookId, cancellationToken);
            if (snapshot != null)
            {
                snapshotDict[snapshot.BookId] = snapshot;
            }
        }

        var books = pagedEntitlements.Select(entitlement =>
        {
            var snapshot = snapshotDict.GetValueOrDefault(entitlement.BookId);
            return new BookDto
            {
                BookId = entitlement.BookId,
                Title = snapshot?.Title ?? "Unknown Book",
                Authors = snapshot?.Authors ?? "Unknown Author",
                CoverRef = snapshot?.CoverRef,
                AcquiredAt = new DateTimeOffset(entitlement.AcquiredAt, TimeSpan.Zero)
            };
        }).ToList();

        return Result.Success(new MyBooksDto
        {
            Books = books,
            TotalCount = totalCount
        });
    }
}
