using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Commands.RelistBook;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Contracts.Catalog.V1;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Commands.RelistBook;

public class RelistBookHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICache> _cache = new();

    private RelistBookHandler CreateHandler() => new(_bookRepository.Object, _outboxWriter.Object, _cache.Object);

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(new RelistBookCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PublishedBook_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));

        var result = await CreateHandler().Handle(new RelistBookCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _bookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnlistedBook_RelistsWritesEventAndInvalidatesCache()
    {
        var bookId = Guid.NewGuid();
        var book = BookFactory.Unlisted(bookId);
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(book);

        var result = await CreateHandler().Handle(new RelistBookCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookStatus.Published, book.Status);
        _bookRepository.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<BookRelistedV1>(e => e.BookId == bookId),
            Contracts.Common.EventTypes.BookRelisted,
            It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
