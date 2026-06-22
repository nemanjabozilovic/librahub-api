using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Access.Commands.CreateReadToken;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Domain.Access;
using LibraHub.Content.Domain.Books;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Access.Commands.CreateReadToken;

public class CreateReadTokenHandlerTests
{
    private readonly Mock<IAccessGrantRepository> _accessGrantRepository = new();
    private readonly Mock<ICatalogReadClient> _catalogClient = new();
    private readonly Mock<ILibraryAccessClient> _libraryClient = new();
    private readonly Mock<IBookEditionRepository> _editionRepository = new();
    private readonly Mock<ICoverRepository> _coverRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ReadAccessOptions _options = new()
    {
        CatalogApiUrl = "https://catalog.test",
        LibraryApiUrl = "https://library.test",
        TokenExpirationMinutes = 30,
        TokenRefreshThresholdMinutes = 5
    };

    private readonly Guid _userId = Guid.NewGuid();

    public CreateReadTokenHandlerTests()
    {
        _currentUser.SetupGet(u => u.UserId).Returns(_userId);
        _currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
        _clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc));
    }

    private CreateReadTokenHandler CreateHandler() => new(
        _accessGrantRepository.Object,
        _catalogClient.Object,
        _libraryClient.Object,
        _editionRepository.Object,
        _coverRepository.Object,
        _currentUser.Object,
        _clock.Object,
        Microsoft.Extensions.Options.Options.Create(_options));

    private void SetupBookInfo(Guid bookId, bool isBlocked = false, bool isFree = false)
    {
        _catalogClient
            .Setup(c => c.GetBookInfoAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new BookInfo { BookId = bookId, IsBlocked = isBlocked, IsFree = isFree }));
    }

    [Fact]
    public async Task Handle_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(u => u.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_BookNotFound_ReturnsFailure()
    {
        var bookId = Guid.NewGuid();
        _catalogClient
            .Setup(c => c.GetBookInfoAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<BookInfo>(Error.NotFound("BOOK_NOT_FOUND")));

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_BookBlocked_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isBlocked: true);

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NoAccess_ReturnsForbidden()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: false);
        _libraryClient
            .Setup(l => l.UserOwnsBookAsync(_userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        _accessGrantRepository.Verify(r => r.AddAsync(It.IsAny<AccessGrant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LibraryAccessCheckFails_ReturnsError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: false);
        _libraryClient
            .Setup(l => l.UserOwnsBookAsync(_userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(Error.Unexpected("library down")));

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNEXPECTED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_FreeBook_CoverScope_CreatesToken()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: true);
        _coverRepository
            .Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cover(Guid.NewGuid(), bookId, Guid.NewGuid()));

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Value));
        _libraryClient.Verify(l => l.UserOwnsBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _accessGrantRepository.Verify(r => r.AddAsync(It.Is<AccessGrant>(g => g.Scope == AccessScope.Cover && g.BookId == bookId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminRole_BypassesOwnershipCheck()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: false);
        _currentUser.Setup(u => u.IsInRole("Admin")).Returns(true);
        _coverRepository
            .Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cover(Guid.NewGuid(), bookId, Guid.NewGuid()));

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _libraryClient.Verify(l => l.UserOwnsBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CoverScope_MissingCover_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: true);
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Cover?)null);

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EditionScope_InvalidFormat_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: true);

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId, "Mobi"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EditionScope_SpecificVersion_CreatesToken()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: true);
        var edition = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Pdf, 2, Guid.NewGuid());
        _editionRepository
            .Setup(r => r.GetByBookIdFormatAndVersionAsync(bookId, BookFormat.Pdf, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edition);

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId, "pdf", 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _accessGrantRepository.Verify(r => r.AddAsync(It.Is<AccessGrant>(g => g.Scope == AccessScope.Edition && g.Format == BookFormat.Pdf && g.Version == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EditionScope_SpecificVersionNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: true);
        _editionRepository
            .Setup(r => r.GetByBookIdFormatAndVersionAsync(bookId, BookFormat.Pdf, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookEdition?)null);

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId, "pdf", 9), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EditionScope_LatestVersion_CreatesTokenWithLatestVersion()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isFree: true);
        var latest = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Epub, 5, Guid.NewGuid());
        _editionRepository
            .Setup(r => r.GetLatestByBookIdAndFormatAsync(bookId, BookFormat.Epub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(latest);

        var result = await CreateHandler().Handle(new CreateReadTokenCommand(bookId, "epub"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _accessGrantRepository.Verify(r => r.AddAsync(It.Is<AccessGrant>(g => g.Version == 5 && g.Format == BookFormat.Epub), It.IsAny<CancellationToken>()), Times.Once);
    }
}
