using LibraHub.Contracts.Catalog.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Consumers;
using LibraHub.Library.Domain.Books;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Consumers;

public class BookPublishedConsumerTests
{
    private readonly Mock<IBookSnapshotStore> _bookSnapshotStore = new();

    private BookPublishedConsumer CreateConsumer() =>
        new(_bookSnapshotStore.Object, NullLogger<BookPublishedConsumer>.Instance);

    private static BookPublishedV1 Event(Guid bookId) => new()
    {
        BookId = bookId,
        Title = "New Title",
        Authors = "New Author",
        PublishedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_NoExistingSnapshot_CreatesSnapshot()
    {
        var bookId = Guid.NewGuid();
        _bookSnapshotStore.Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookSnapshot?)null);

        BookSnapshot? saved = null;
        _bookSnapshotStore
            .Setup(s => s.AddOrUpdateAsync(It.IsAny<BookSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BookSnapshot, CancellationToken>((b, _) => saved = b)
            .Returns(Task.CompletedTask);

        await CreateConsumer().HandleAsync(Event(bookId), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(bookId, saved!.BookId);
        Assert.Equal("New Title", saved.Title);
        Assert.Equal("New Author", saved.Authors);
    }

    [Fact]
    public async Task Handle_ExistingSnapshot_UpdatesIt()
    {
        var bookId = Guid.NewGuid();
        var existing = new BookSnapshot(bookId, "Old Title", "Old Author");
        _bookSnapshotStore.Setup(s => s.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await CreateConsumer().HandleAsync(Event(bookId), CancellationToken.None);

        Assert.Equal("New Title", existing.Title);
        Assert.Equal("New Author", existing.Authors);
        _bookSnapshotStore.Verify(s => s.AddOrUpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }
}
