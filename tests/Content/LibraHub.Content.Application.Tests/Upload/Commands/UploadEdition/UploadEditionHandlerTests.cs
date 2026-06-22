using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Application.Tests.TestHelpers;
using LibraHub.Content.Application.Upload.Commands.UploadEdition;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Storage;
using LibraHub.Contracts.Content.V1;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Upload.Commands.UploadEdition;

public class UploadEditionHandlerTests
{
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IStoredObjectRepository> _storedObjectRepository = new();
    private readonly Mock<IBookEditionRepository> _editionRepository = new();
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

    public UploadEditionHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(_now);
    }

    private UploadEditionHandler CreateHandler() => new(
        _objectStorage.Object,
        _storedObjectRepository.Object,
        _editionRepository.Object,
        _catalogClient.Object,
        _outboxWriter.Object,
        _clock.Object,
        Microsoft.Extensions.Options.Options.Create(_uploadOptions));

    private static UploadEditionCommand CreateCommand(Guid bookId, string format = "Pdf")
        => new(bookId, new InMemoryFormFile(new byte[] { 1, 2, 3 }, "application/pdf", "book.pdf"), format);

    private void SetupBookInfo(Guid bookId, bool isBlocked = false)
    {
        _catalogClient
            .Setup(c => c.GetBookInfoAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new BookInfo { BookId = bookId, IsBlocked = isBlocked }));
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
    public async Task Handle_InvalidFormat_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);

        var result = await CreateHandler().Handle(CreateCommand(bookId, "Mobi"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Equal("EDITION_INVALID_FORMAT", result.Error!.Message);
    }

    [Fact]
    public async Task Handle_FirstEdition_VersionOne_StoresAndWritesEvent()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);
        _editionRepository
            .Setup(r => r.GetLatestByBookIdAndFormatAsync(bookId, BookFormat.Pdf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookEdition?)null);

        var result = await CreateHandler().Handle(CreateCommand(bookId, "pdf"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _objectStorage.Verify(s => s.UploadAsync("editions", It.IsAny<string>(), It.IsAny<Stream>(), "application/pdf", It.IsAny<CancellationToken>()), Times.Once);
        _storedObjectRepository.Verify(r => r.AddAsync(It.IsAny<StoredObject>(), It.IsAny<CancellationToken>()), Times.Once);
        _editionRepository.Verify(r => r.AddAsync(It.IsAny<BookEdition>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<EditionUploadedV1>(e => e.BookId == bookId && e.Format == "Pdf" && e.Version == 1 && e.UploadedAt == _now),
            Contracts.Common.EventTypes.EditionUploaded,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingLatest_IncrementsVersion()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);
        var latest = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Pdf, 3, Guid.NewGuid());
        _editionRepository
            .Setup(r => r.GetLatestByBookIdAndFormatAsync(bookId, BookFormat.Pdf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(latest);

        var result = await CreateHandler().Handle(CreateCommand(bookId, "pdf"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<EditionUploadedV1>(e => e.Version == 4),
            Contracts.Common.EventTypes.EditionUploaded,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UploadThrows_ReturnsInternalError()
    {
        var bookId = Guid.NewGuid();
        SetupBookInfo(bookId);
        _editionRepository
            .Setup(r => r.GetLatestByBookIdAndFormatAsync(bookId, BookFormat.Pdf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookEdition?)null);
        _objectStorage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await CreateHandler().Handle(CreateCommand(bookId, "pdf"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INTERNAL_ERROR", result.Error!.Code);
        _editionRepository.Verify(r => r.AddAsync(It.IsAny<BookEdition>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EditionUploadedV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
