using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Commands.RemoveBook;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Contracts.Catalog.V1;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Commands.RemoveBook;

public class RemoveBookHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICache> _cache = new();

    private RemoveBookHandler CreateHandler() => new(
        _bookRepository.Object,
        _currentUser.Object,
        _outboxWriter.Object,
        _cache.Object);

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(new RemoveBookCommand(bookId, "reason"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedUser_ReturnsUnauthorized()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _currentUser.SetupGet(u => u.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new RemoveBookCommand(bookId, "reason"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EmptyReason_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _currentUser.SetupGet(u => u.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(new RemoveBookCommand(bookId, "  "), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _bookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRemoval_RemovesWritesEventAndInvalidatesCache()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var book = BookFactory.Draft(bookId);
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _currentUser.SetupGet(u => u.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new RemoveBookCommand(bookId, "policy"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookStatus.Removed, book.Status);
        _bookRepository.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<BookRemovedV1>(e => e.BookId == bookId && e.RemovedBy == userId && e.Reason == "policy"),
            Contracts.Common.EventTypes.BookRemoved,
            It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
