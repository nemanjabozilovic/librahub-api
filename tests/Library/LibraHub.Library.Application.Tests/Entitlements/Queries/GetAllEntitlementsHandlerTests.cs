using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Entitlements.Queries.GetAllEntitlements;
using LibraHub.Library.Domain.Books;
using LibraHub.Library.Domain.Entitlements;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Entitlements.Queries;

public class GetAllEntitlementsHandlerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IIdentityClient> _identityClient = new();
    private readonly Mock<IBookSnapshotStore> _bookSnapshotStore = new();

    private GetAllEntitlementsHandler CreateHandler() =>
        new(_entitlementRepository.Object, _identityClient.Object, _bookSnapshotStore.Object);

    [Fact]
    public async Task Handle_InvalidPage_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(
            new GetAllEntitlementsQuery { Page = 0 }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidPageSize_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(
            new GetAllEntitlementsQuery { Page = 1, PageSize = 0 }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidStatus_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(
            new GetAllEntitlementsQuery { Page = 1, PageSize = 20, Status = "Bogus" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidSource_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(
            new GetAllEntitlementsQuery { Page = 1, PageSize = 20, Source = "Bogus" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_Valid_ReturnsPagedResultWithEnrichment()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);

        _entitlementRepository
            .Setup(r => r.GetAllAsync(0, 20, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { entitlement });
        _entitlementRepository
            .Setup(r => r.CountAllAsync(null, null, null, null, (DateTime?)null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var userDict = new Dictionary<Guid, UserInfo?>
        {
            [userId] = new UserInfo { Id = userId, Email = "u@e.com", DisplayName = "User" }
        };
        _identityClient
            .Setup(c => c.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userDict));

        _bookSnapshotStore
            .Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookSnapshot(bookId, "Book Title", "Author"));

        var result = await CreateHandler().Handle(
            new GetAllEntitlementsQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        var summary = Assert.Single(result.Value.Entitlements);
        Assert.Equal("User", summary.UserDisplayName);
        Assert.Equal("u@e.com", summary.UserEmail);
        Assert.Equal("Book Title", summary.BookTitle);
        Assert.Equal("Active", summary.Status);
    }

    [Fact]
    public async Task Handle_IdentityClientFails_StillReturnsEntitlementsWithoutUserInfo()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.AdminGrant);

        _entitlementRepository
            .Setup(r => r.GetAllAsync(0, 20, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { entitlement });
        _entitlementRepository
            .Setup(r => r.CountAllAsync(null, null, null, null, (DateTime?)null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _identityClient
            .Setup(c => c.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Dictionary<Guid, UserInfo?>>(Error.Unexpected("down")));
        _bookSnapshotStore
            .Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookSnapshot?)null);

        var result = await CreateHandler().Handle(
            new GetAllEntitlementsQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var summary = Assert.Single(result.Value.Entitlements);
        Assert.Null(summary.UserDisplayName);
        Assert.Null(summary.BookTitle);
    }
}
