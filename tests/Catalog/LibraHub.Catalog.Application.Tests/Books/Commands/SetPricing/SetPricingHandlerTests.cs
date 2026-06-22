using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Commands.SetPricing;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Contracts.Catalog.V1;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Commands.SetPricing;

public class SetPricingHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IPricingRepository> _pricingRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICache> _cache = new();

    private SetPricingHandler CreateHandler() => new(
        _bookRepository.Object,
        _pricingRepository.Object,
        _outboxWriter.Object,
        _cache.Object);

    private static SetPricingCommand CreateCommand(
        Guid bookId,
        decimal? promoPrice = null,
        string? promoName = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null) => new(
        bookId, 20m, Currency.USD, 10m, promoPrice, promoName, start, end);

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NoExistingPricing_AddsNewPolicyAndWritesEvent()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((PricingPolicy?)null);

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _pricingRepository.Verify(r => r.AddAsync(It.Is<PricingPolicy>(p => p.BookId == bookId && p.Price.Amount == 20m), It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<BookPricingChangedV1>(e => e.BookId == bookId && e.Price == 20m && e.Currency == Currency.USD),
            Contracts.Common.EventTypes.BookPricingChanged,
            It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ExistingPricing_UpdatesPolicy()
    {
        var bookId = Guid.NewGuid();
        var existing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(5m, Currency.USD), 5m);
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(20m, existing.Price.Amount);
        _pricingRepository.Verify(r => r.AddAsync(It.IsAny<PricingPolicy>(), It.IsAny<CancellationToken>()), Times.Never);
        _pricingRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_FullPromo_SetsPromoOnPolicy()
    {
        var bookId = Guid.NewGuid();
        var existing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(20m, Currency.USD));
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var command = CreateCommand(bookId, promoPrice: 10m, promoName: "Sale", start: start, end: end);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(existing.PromoPrice);
        Assert.Equal(10m, existing.PromoPrice!.Amount);
        Assert.Equal("Sale", existing.PromoName);
    }

    [Fact]
    public async Task Handle_PromoStartAfterEnd_ThrowsArgumentException()
    {
        var bookId = Guid.NewGuid();
        var existing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(20m, Currency.USD));
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var command = CreateCommand(bookId, promoPrice: 10m, promoName: "Sale", start: start, end: end);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PromoCleared_WhenExistingPromoAndNoneRequested()
    {
        var bookId = Guid.NewGuid();
        var existing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(20m, Currency.USD));
        existing.SetPromo(
            new Money(10m, Currency.USD),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 2, 1),
            "OldSale");
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await CreateHandler().Handle(CreateCommand(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(existing.PromoPrice);
        Assert.Null(existing.PromoName);
    }
}
