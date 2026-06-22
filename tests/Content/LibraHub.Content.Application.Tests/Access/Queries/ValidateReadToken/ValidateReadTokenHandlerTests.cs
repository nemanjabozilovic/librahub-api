using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Access.Queries.ValidateReadToken;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Domain.Access;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Storage;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Access.Queries.ValidateReadToken;

public class ValidateReadTokenHandlerTests
{
    private readonly Mock<IAccessGrantRepository> _accessGrantRepository = new();
    private readonly Mock<IStoredObjectRepository> _storedObjectRepository = new();
    private readonly Mock<IBookEditionRepository> _editionRepository = new();
    private readonly Mock<ICoverRepository> _coverRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ReadAccessOptions _options = new()
    {
        CatalogApiUrl = "https://catalog.test",
        LibraryApiUrl = "https://library.test",
        TokenExpirationMinutes = 30,
        TokenRefreshThresholdMinutes = 5
    };

    // AccessGrant sets IssuedAt to the real DateTime.UtcNow in its constructor, so the clock must be at or after that moment.
    private readonly DateTime _now = DateTime.UtcNow.AddMinutes(1);

    public ValidateReadTokenHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(_now);
    }

    private ValidateReadTokenHandler CreateHandler() => new(
        _accessGrantRepository.Object,
        _storedObjectRepository.Object,
        _editionRepository.Object,
        _coverRepository.Object,
        _clock.Object,
        Microsoft.Extensions.Options.Options.Create(_options));

    private static AccessGrant CreateCoverGrant(Guid bookId, DateTime expiresAt)
        => new(Guid.NewGuid(), "tok", bookId, null, null, AccessScope.Cover, Guid.NewGuid(), expiresAt);

    private static AccessGrant CreateEditionGrant(Guid bookId, BookFormat format, int version, DateTime expiresAt)
        => new(Guid.NewGuid(), "tok", bookId, format, version, AccessScope.Edition, Guid.NewGuid(), expiresAt);

    private static StoredObject CreateStoredObject(Guid bookId)
        => new(Guid.NewGuid(), bookId, "books/x/object.pdf", "application/pdf", 123, new Sha256(new string('a', 64)));

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsNotFound()
    {
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync((AccessGrant?)null);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        Assert.Equal("ACCESS_TOKEN_INVALID not found", result.Error!.Message);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        var grant = CreateCoverGrant(bookId, _now.AddMinutes(-1));
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Equal("ACCESS_TOKEN_EXPIRED", result.Error!.Message);
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsValidationErrorRevoked()
    {
        var bookId = Guid.NewGuid();
        var grant = CreateCoverGrant(bookId, _now.AddMinutes(30));
        grant.Revoke();
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Equal("ACCESS_TOKEN_REVOKED", result.Error!.Message);
    }

    [Fact]
    public async Task Handle_ValidCoverGrant_ReturnsInfo()
    {
        var bookId = Guid.NewGuid();
        var grant = CreateCoverGrant(bookId, _now.AddMinutes(30));
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);

        var cover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(cover);
        var stored = CreateStoredObject(bookId);
        _storedObjectRepository.Setup(s => s.GetByIdAsync(cover.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(bookId, result.Value.BookId);
        Assert.Equal("Cover", result.Value.Scope);
        Assert.Equal(stored.ObjectKey, result.Value.ObjectKey);
        Assert.Equal(stored.SizeBytes, result.Value.SizeBytes);
    }

    [Fact]
    public async Task Handle_NearExpiry_RefreshesAndUpdates()
    {
        var bookId = Guid.NewGuid();
        // expires in 2 minutes, threshold is 5 -> near expiry
        var grant = CreateCoverGrant(bookId, _now.AddMinutes(2));
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);

        var cover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(cover);
        _storedObjectRepository.Setup(s => s.GetByIdAsync(cover.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateStoredObject(bookId));

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _accessGrantRepository.Verify(r => r.UpdateAsync(grant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CoverGrant_MissingCover_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        var grant = CreateCoverGrant(bookId, _now.AddMinutes(30));
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Cover?)null);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidEditionGrant_ReturnsInfo()
    {
        var bookId = Guid.NewGuid();
        var grant = CreateEditionGrant(bookId, BookFormat.Pdf, 2, _now.AddMinutes(30));
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);

        var edition = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Pdf, 2, Guid.NewGuid());
        _editionRepository.Setup(r => r.GetByBookIdFormatAndVersionAsync(bookId, BookFormat.Pdf, 2, It.IsAny<CancellationToken>())).ReturnsAsync(edition);
        var stored = CreateStoredObject(bookId);
        _storedObjectRepository.Setup(s => s.GetByIdAsync(edition.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Edition", result.Value.Scope);
        Assert.Equal("Pdf", result.Value.Format);
        Assert.Equal(2, result.Value.Version);
    }

    [Fact]
    public async Task Handle_EditionGrant_EditionNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        var grant = CreateEditionGrant(bookId, BookFormat.Pdf, 2, _now.AddMinutes(30));
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        _editionRepository.Setup(r => r.GetByBookIdFormatAndVersionAsync(bookId, BookFormat.Pdf, 2, It.IsAny<CancellationToken>())).ReturnsAsync((BookEdition?)null);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_StoredObjectMissing_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        var grant = CreateCoverGrant(bookId, _now.AddMinutes(30));
        _accessGrantRepository.Setup(r => r.GetByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(grant);
        var cover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(cover);
        _storedObjectRepository.Setup(s => s.GetByIdAsync(cover.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync((StoredObject?)null);

        var result = await CreateHandler().Handle(new ValidateReadTokenQuery("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }
}
