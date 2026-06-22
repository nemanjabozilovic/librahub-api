using LibraHub.BuildingBlocks.Constants;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Queries.GetBookInfo;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Queries.GetBookInfo;

public class GetBookInfoHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IPricingRepository> _pricingRepository = new();

    private GetBookInfoHandler CreateHandler() => new(_bookRepository.Object, _pricingRepository.Object);

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(new GetBookInfoQuery(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_FreeBook_ReturnsIsFreeTrue()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingPolicy(Guid.NewGuid(), bookId, new Money(0m, Currency.USD)));

        var result = await CreateHandler().Handle(new GetBookInfoQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsFree);
        Assert.False(result.Value.IsBlocked);
    }

    [Fact]
    public async Task Handle_RemovedBook_ReturnsIsBlockedTrue()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Removed(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingPolicy(Guid.NewGuid(), bookId, new Money(10m, Currency.USD)));

        var result = await CreateHandler().Handle(new GetBookInfoQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsBlocked);
        Assert.False(result.Value.IsFree);
    }
}
