using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Commands.PublishBook;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Projections;
using LibraHub.Contracts.Catalog.V1;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Commands.PublishBook;

public class PublishBookHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IPricingRepository> _pricingRepository = new();
    private readonly Mock<IBookContentStateRepository> _contentStateRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICache> _cache = new();

    public PublishBookHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private PublishBookHandler CreateHandler() => new(
        _bookRepository.Object,
        _pricingRepository.Object,
        _contentStateRepository.Object,
        _outboxWriter.Object,
        _unitOfWork.Object,
        _cache.Object);

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(new PublishBookCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PricingMissing_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((PricingPolicy?)null);

        var result = await CreateHandler().Handle(new PublishBookCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ContentNotReady_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingPolicy(Guid.NewGuid(), bookId, new Money(10m, "USD")));
        _contentStateRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookContentState?)null);

        var result = await CreateHandler().Handle(new PublishBookCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _bookRepository.Verify(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidDraft_PublishesWritesEventAndInvalidatesCache()
    {
        var bookId = Guid.NewGuid();
        var book = BookFactory.Draft(bookId);
        var (pricing, content) = BookFactory.PublishablePrerequisites(bookId);

        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(pricing);
        _contentStateRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(content);

        var result = await CreateHandler().Handle(new PublishBookCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookStatus.Published, book.Status);
        _bookRepository.Verify(r => r.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<BookPublishedV1>(e => e.BookId == bookId),
            Contracts.Common.EventTypes.BookPublished,
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
