using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Consumers;
using LibraHub.Library.Domain.Books;
using LibraHub.Library.Domain.Entitlements;
using LibraHub.Library.Domain.Reading;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Consumers;

public class BookRemovedConsumerTests
{
    private readonly Mock<IBookSnapshotStore> _bookSnapshotStore = new();
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IReadingProgressRepository> _readingProgressRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public BookRemovedConsumerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private BookRemovedConsumer CreateConsumer() => new(
        _bookSnapshotStore.Object,
        _entitlementRepository.Object,
        _readingProgressRepository.Object,
        _unitOfWork.Object,
        NullLogger<BookRemovedConsumer>.Instance);

    private static BookRemovedV1 Event(Guid bookId) => new()
    {
        BookId = bookId,
        Title = "Removed Book",
        Reason = "policy",
        RemovedBy = Guid.NewGuid(),
        RemovedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_RemovesSnapshotRevokesEntitlementsDeletesProgress()
    {
        var bookId = Guid.NewGuid();
        var snapshot = new BookSnapshot(bookId, "Title", "Author");
        _bookSnapshotStore.Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var entitlement = new Entitlement(Guid.NewGuid(), Guid.NewGuid(), bookId, EntitlementSource.Purchase);
        _entitlementRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { entitlement });

        var progress = new ReadingProgress(Guid.NewGuid(), Guid.NewGuid(), bookId);
        _readingProgressRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadingProgress> { progress });

        await CreateConsumer().HandleAsync(Event(bookId), CancellationToken.None);

        Assert.Equal(BookAvailability.Removed, snapshot.Availability);
        _bookSnapshotStore.Verify(s => s.AddOrUpdateAsync(snapshot, It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(entitlement.IsActive);
        _entitlementRepository.Verify(r => r.UpdateAsync(entitlement, It.IsAny<CancellationToken>()), Times.Once);
        _readingProgressRepository.Verify(r => r.DeleteAsync(progress, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoSnapshotNoEntitlementsNoProgress_NoMutations()
    {
        var bookId = Guid.NewGuid();
        _bookSnapshotStore.Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookSnapshot?)null);
        _entitlementRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Entitlement>());
        _readingProgressRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReadingProgress>());

        await CreateConsumer().HandleAsync(Event(bookId), CancellationToken.None);

        _bookSnapshotStore.Verify(s => s.AddOrUpdateAsync(It.IsAny<BookSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
        _entitlementRepository.Verify(r => r.UpdateAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _readingProgressRepository.Verify(r => r.DeleteAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
