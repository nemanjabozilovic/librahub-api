using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Application.Tests.TestHelpers;
using LibraHub.Content.Application.Upload.Commands.UploadCover;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Storage;
using LibraHub.Contracts.Content.V1;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Upload.Commands.UploadCover;

public class UploadCoverHandlerTests
{
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IStoredObjectRepository> _storedObjectRepository = new();
    private readonly Mock<ICoverRepository> _coverRepository = new();
    private readonly Mock<ICatalogReadClient> _catalogClient = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IClock> _clock = new();
    private readonly UploadOptions _uploadOptions = new()
    {
        CoversBucketName = "covers",
        EditionsBucketName = "editions",
        MaxCoverSizeBytes = 1_000_000,
        MaxEditionSizeBytes = 1_000_000
    };

    private readonly DateTimeOffset _now = new(2026, 6, 22, 10, 0, 0, TimeSpan.Zero);

    public UploadCoverHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(_now);
    }

    private UploadCoverHandler CreateHandler() => new(
        _objectStorage.Object,
        _storedObjectRepository.Object,
        _coverRepository.Object,
        _catalogClient.Object,
        _outboxWriter.Object,
        _clock.Object,
        Microsoft.Extensions.Options.Options.Create(_uploadOptions));

    private static UploadCoverCommand CreateCommand(Guid bookId)
        => new(bookId, new InMemoryFormFile(new byte[] { 1, 2, 3 }, "image/png", "cover.png"));

    private void SetupBookInfo(Guid bookId, bool isBlocked = false, bool isFree = false)
    {
        _catalogClient
            .Setup(c => c.GetBookInfoAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new BookInfo { BookId = bookId, IsBlocked = isBlocked, IsFree = isFree }));
    }

    [Fact]
    public async Task Handle_BookNotFound_ReturnsFailure()
    {
        var bookId = Guid.NewGuid();
        _catalogClient
            .Setup(c => c.GetBookInfoAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<BookInfo>(Error.NotFound("BOOK_NOT_FOUND")));

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        _objectStorage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BookBlocked_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId, isBlocked: true);

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NewCover_StoresObjectAndWritesEvent()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Cover?)null);

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        _objectStorage.Verify(s => s.UploadAsync("covers", It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
        _storedObjectRepository.Verify(r => r.AddAsync(It.IsAny<StoredObject>(), It.IsAny<CancellationToken>()), Times.Once);
        _coverRepository.Verify(r => r.AddAsync(It.IsAny<Cover>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<CoverUploadedV1>(e => e.BookId == bookId && e.ContentType == "image/png" && e.UploadedAt == _now),
            Contracts.Common.EventTypes.CoverUploaded,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCover_DeletesOldThenUploadsNew()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);

        var existingCover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        var existingStored = new StoredObject(Guid.NewGuid(), bookId, "books/x/cover/old.png", "image/png", 10, new Sha256(new string('a', 64)));
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existingCover);
        _storedObjectRepository.Setup(r => r.GetByIdAsync(existingCover.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingStored);

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _objectStorage.Verify(s => s.DeleteAsync("covers", existingStored.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
        _coverRepository.Verify(r => r.DeleteAsync(existingCover, It.IsAny<CancellationToken>()), Times.Once);
        _storedObjectRepository.Verify(r => r.DeleteAsync(existingStored, It.IsAny<CancellationToken>()), Times.Once);
        _coverRepository.Verify(r => r.AddAsync(It.IsAny<Cover>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCoverDeleteFails_ReturnsInternalError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);

        var existingCover = new Cover(Guid.NewGuid(), bookId, Guid.NewGuid());
        var existingStored = new StoredObject(Guid.NewGuid(), bookId, "books/x/cover/old.png", "image/png", 10, new Sha256(new string('a', 64)));
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existingCover);
        _storedObjectRepository.Setup(r => r.GetByIdAsync(existingCover.StoredObjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingStored);
        _objectStorage.Setup(s => s.DeleteAsync("covers", existingStored.ObjectKey, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INTERNAL_ERROR", result.Error!.Code);
        _coverRepository.Verify(r => r.AddAsync(It.IsAny<Cover>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UploadThrows_ReturnsInternalError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);
        _coverRepository.Setup(c => c.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Cover?)null);
        _objectStorage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INTERNAL_ERROR", result.Error!.Code);
        _storedObjectRepository.Verify(r => r.AddAsync(It.IsAny<StoredObject>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<CoverUploadedV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
