using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Queries.GetOrderPricingQuote;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Books.Queries.GetOrderPricingQuote;

public class GetOrderPricingQuoteHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IPricingRepository> _pricingRepository = new();
    private readonly Mock<IClock> _clock = new();

    private readonly DateTime _now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    public GetOrderPricingQuoteHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(_now);
    }

    private GetOrderPricingQuoteHandler CreateHandler() => new(
        _bookRepository.Object,
        _pricingRepository.Object,
        _clock.Object);

    [Fact]
    public async Task Handle_EmptyBookIds_ReturnsEmptyQuote()
    {
        var result = await CreateHandler().Handle(new GetOrderPricingQuoteQuery(new List<Guid>()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(Currency.USD, result.Value.Currency);
    }

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(new GetOrderPricingQuoteQuery(new List<Guid> { bookId }), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PricingNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((PricingPolicy?)null);

        var result = await CreateHandler().Handle(new GetOrderPricingQuoteQuery(new List<Guid> { bookId }), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NonUsdCurrency_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingPolicy(Guid.NewGuid(), bookId, new Money(10m, "EUR")));

        var result = await CreateHandler().Handle(new GetOrderPricingQuoteQuery(new List<Guid> { bookId }), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PublishedBookWithActivePromo_AppliesDiscount()
    {
        var bookId = Guid.NewGuid();
        var pricing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(20m, Currency.USD), 10m);
        pricing.SetPromo(new Money(12m, Currency.USD), _now.AddDays(-1), _now.AddDays(1), "Sale");

        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId, "Promo Book"));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(pricing);

        var result = await CreateHandler().Handle(new GetOrderPricingQuoteQuery(new List<Guid> { bookId }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.True(item.IsPublished);
        Assert.Equal(20m, item.BasePrice);
        Assert.Equal(12m, item.FinalPrice);
        Assert.Equal("Sale", item.PromotionName);
        Assert.Equal(8m, item.DiscountAmount);
    }

    [Fact]
    public async Task Handle_DraftBookWithPromo_PromoNotApplied()
    {
        var bookId = Guid.NewGuid();
        var pricing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(20m, Currency.USD));
        pricing.SetPromo(new Money(12m, Currency.USD), _now.AddDays(-1), _now.AddDays(1), "Sale");

        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Draft(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(pricing);

        var result = await CreateHandler().Handle(new GetOrderPricingQuoteQuery(new List<Guid> { bookId }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.False(item.IsPublished);
        Assert.Equal(20m, item.FinalPrice);
        Assert.Null(item.DiscountAmount);
    }
}
