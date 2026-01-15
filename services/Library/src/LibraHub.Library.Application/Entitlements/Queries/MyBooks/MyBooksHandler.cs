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
    ICatalogClient catalogClient,
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

        var catalogDictResult = await catalogClient.GetBookDetailsByIdsAsync(bookIds, cancellationToken);
        var catalogDict = catalogDictResult.IsSuccess
            ? catalogDictResult.Value
            : new Dictionary<Guid, CatalogBookDetailsDto>();

        var books = pagedEntitlements.Select(entitlement =>
        {
            var snapshot = snapshotDict.GetValueOrDefault(entitlement.BookId);
            var catalog = catalogDict.GetValueOrDefault(entitlement.BookId);
            var coverUrl = catalog?.CoverUrl;
            return new BookDto
            {
                BookId = entitlement.BookId,
                Title = catalog?.Title ?? snapshot?.Title ?? "Unknown Book",
                Description = catalog?.Description,
                Authors = catalog != null && catalog.Authors.Count > 0
                    ? string.Join(", ", catalog.Authors)
                    : snapshot?.Authors ?? "Unknown Author",
                Categories = catalog?.Categories ?? new List<string>(),
                Tags = catalog?.Tags ?? new List<string>(),
                CoverUrl = coverUrl,
                HasEdition = catalog?.HasEdition ?? false,
                Editions = catalog?.Editions?.Select(e => new BookEditionDto
                {
                    Id = e.Id,
                    Format = e.Format,
                    Version = e.Version,
                    UploadedAt = e.UploadedAt
                }).ToList() ?? new List<BookEditionDto>(),
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
