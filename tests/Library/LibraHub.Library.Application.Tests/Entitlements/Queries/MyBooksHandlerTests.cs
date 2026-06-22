using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Entitlements.Queries.MyBooks;
using LibraHub.Library.Domain.Books;
using LibraHub.Library.Domain.Entitlements;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Entitlements.Queries;

public class MyBooksHandlerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IBookSnapshotStore> _bookSnapshotStore = new();
    private readonly Mock<ICatalogClient> _catalogClient = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private MyBooksHandler CreateHandler() =>
        new(_entitlementRepository.Object, _bookSnapshotStore.Object, _catalogClient.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new MyBooksQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_MergesCatalogOverSnapshot_AndReturnsBooks()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);

        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        _entitlementRepository
            .Setup(r => r.CountActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _entitlementRepository
            .Setup(r => r.GetActiveByUserIdPagedAsync(userId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { entitlement });

        _bookSnapshotStore
            .Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookSnapshot(bookId, "Snapshot Title", "Snapshot Author"));

        var catalogDict = new Dictionary<Guid, CatalogBookDetailsDto>
        {
            [bookId] = new CatalogBookDetailsDto
            {
                Id = bookId,
                Title = "Catalog Title",
                Authors = new List<string> { "A1", "A2" },
                CoverUrl = "http://cover"
            }
        };
        _catalogClient
            .Setup(c => c.GetBookDetailsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(catalogDict));

        var result = await CreateHandler().Handle(new MyBooksQuery { Skip = 0, Take = 20 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        var book = Assert.Single(result.Value.Books);
        Assert.Equal("Catalog Title", book.Title);
        Assert.Equal("A1, A2", book.Authors);
        Assert.Equal("http://cover", book.CoverUrl);
    }

    [Fact]
    public async Task Handle_CatalogFailsAndNoSnapshot_FallsBackToUnknown()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);

        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        _entitlementRepository
            .Setup(r => r.CountActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _entitlementRepository
            .Setup(r => r.GetActiveByUserIdPagedAsync(userId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { entitlement });

        _bookSnapshotStore
            .Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookSnapshot?)null);
        _catalogClient
            .Setup(c => c.GetBookDetailsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Dictionary<Guid, CatalogBookDetailsDto>>(Error.Unexpected("down")));

        var result = await CreateHandler().Handle(new MyBooksQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var book = Assert.Single(result.Value.Books);
        Assert.Equal("Unknown Book", book.Title);
        Assert.Equal("Unknown Author", book.Authors);
    }
}
