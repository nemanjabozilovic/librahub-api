using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Consumers;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Storage;
using LibraHub.Contracts.Catalog.V1;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Content.Application.Tests.Consumers;

public class BookRemovedConsumerTests
{
    private readonly Mock<IStoredObjectRepository> _storedObjectRepository = new();
    private readonly Mock<IBookEditionRepository> _editionRepository = new();
    private readonly Mock<ICoverRepository> _coverRepository = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<BookRemovedConsumer>> _logger = new();
    private readonly UploadOptions _uploadOptions = new()
    {
        CoversBucketName = "covers",
        EditionsBucketName = "editions",
        MaxCoverSizeBytes = 1_000_000,
        MaxEditionSizeBytes = 1_000_000
    };

    public BookRemovedConsumerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private BookRemovedConsumer CreateConsumer() => new(
        _storedObjectRepository.Object,
        _editionRepository.Object,
        _coverRepository.Object,
        _objectStorage.Object,
        _unitOfWork.Object,
        Microsoft.Extensions.Options.Options.Create(_uploadOptions),
        _logger.Object);

    private static StoredObject CreateStored(Guid bookId, string objectKey)
        => new(Guid.NewGuid(), bookId, objectKey, "application/pdf", 10, new Sha256(new string('a', 64)));

    [Fact]
    public async Task HandleAsync_DeletesStorageWithCorrectBucketsAndPurgesRepositories()
    {
        var bookId = Guid.NewGuid();
        var coverStored = CreateStored(bookId, "books/x/cover/c.png");
        var editionStored = CreateStored(bookId, "books/x/editions/pdf/v1/e.pdf");
        var cover = new Cover(Guid.NewGuid(), bookId, coverStored.Id);
        var edition = new BookEdition(Guid.NewGuid(), bookId, BookFormat.Pdf, 1, editionStored.Id);

        _storedObjectRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoredObject> { coverStored, editionStored });
        _editionRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookEdition> { edition });
        _coverRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(cover);

        var @event = new BookRemovedV1 { BookId = bookId, Reason = "policy" };
        await CreateConsumer().HandleAsync(@event, CancellationToken.None);

        _objectStorage.Verify(s => s.DeleteAsync("covers", coverStored.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
        _objectStorage.Verify(s => s.DeleteAsync("editions", editionStored.ObjectKey, It.IsAny<CancellationToken>()), Times.Once);
        _coverRepository.Verify(r => r.DeleteAsync(cover, It.IsAny<CancellationToken>()), Times.Once);
        _editionRepository.Verify(r => r.DeleteAsync(edition, It.IsAny<CancellationToken>()), Times.Once);
        _storedObjectRepository.Verify(r => r.DeleteAsync(coverStored, It.IsAny<CancellationToken>()), Times.Once);
        _storedObjectRepository.Verify(r => r.DeleteAsync(editionStored, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StorageDeleteThrows_StillPurgesRepositories()
    {
        var bookId = Guid.NewGuid();
        var stored = CreateStored(bookId, "books/x/editions/pdf/v1/e.pdf");
        _storedObjectRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoredObject> { stored });
        _editionRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<BookEdition>());
        _coverRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Cover?)null);
        _objectStorage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var @event = new BookRemovedV1 { BookId = bookId, Reason = "policy" };
        await CreateConsumer().HandleAsync(@event, CancellationToken.None);

        _storedObjectRepository.Verify(r => r.DeleteAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NoCover_DoesNotDeleteCover()
    {
        var bookId = Guid.NewGuid();
        _storedObjectRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<StoredObject>());
        _editionRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<BookEdition>());
        _coverRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Cover?)null);

        var @event = new BookRemovedV1 { BookId = bookId, Reason = "policy" };
        await CreateConsumer().HandleAsync(@event, CancellationToken.None);

        _coverRepository.Verify(r => r.DeleteAsync(It.IsAny<Cover>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
