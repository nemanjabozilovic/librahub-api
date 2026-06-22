using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Commands.UpdateBook;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Contracts.Catalog.V1;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Commands.UpdateBook;

public class UpdateBookHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICache> _cache = new();

    private UpdateBookHandler CreateHandler() => new(_bookRepository.Object, _outboxWriter.Object, _cache.Object);

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(new UpdateBookCommand(bookId, "T", null, null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidIsbn_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));

        var result = await CreateHandler().Handle(new UpdateBookCommand(bookId, null, null, null, null, null, "bad", null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _bookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidUpdate_PersistsWritesEventAndInvalidatesCache()
    {
        var bookId = Guid.NewGuid();
        var book = BookFactory.Draft(bookId, "Old");
        book.AddAuthor("A1");
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(book);

        var command = new UpdateBookCommand(
            bookId, "New Title", "New Desc", "sr", "Pub",
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), "1234567890",
            new List<string> { "A2" },
            new List<string> { "Cat" },
            new List<string> { "Tag" });

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", book.Title);
        Assert.Contains(book.Authors, a => a.Name == "A2");
        Assert.DoesNotContain(book.Authors, a => a.Name == "A1");

        _bookRepository.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<BookUpdatedV1>(e => e.BookId == bookId && e.Title == "New Title"),
            Contracts.Common.EventTypes.BookUpdated,
            It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
