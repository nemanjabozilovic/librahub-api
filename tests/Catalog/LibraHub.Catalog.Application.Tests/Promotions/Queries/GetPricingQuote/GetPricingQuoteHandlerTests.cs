using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Promotions.Queries.GetPricingQuote;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Books;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Promotions.Queries.GetPricingQuote;

public class GetPricingQuoteHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IPricingRepository> _pricingRepository = new();
    private readonly Mock<IClock> _clock = new();

    private readonly DateTime _now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    public GetPricingQuoteHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(_now);
    }

    private GetPricingQuoteHandler CreateHandler() => new(
        _bookRepository.Object,
        _pricingRepository.Object,
        _clock.Object);

    private static GetPricingQuoteQuery Query(Guid bookId, string currency = Currency.USD) => new(
        currency,
        new List<PricingQuoteItemRequest> { new() { BookId = bookId } });

    [Fact]
    public async Task Handle_NonUsdCurrency_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(Query(Guid.NewGuid(), "EUR"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_BookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Book?)null);

        var result = await CreateHandler().Handle(Query(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PricingNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((PricingPolicy?)null);

        var result = await CreateHandler().Handle(Query(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_VatApplied_WithoutPromo()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingPolicy(Guid.NewGuid(), bookId, new Money(100m, Currency.USD), 20m));

        var result = await CreateHandler().Handle(Query(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(100m, item.BasePrice);
        Assert.Equal(120m, item.FinalPrice);
        Assert.Null(item.AppliedPromotion);
    }

    [Fact]
    public async Task Handle_ActivePromo_AppliesDiscountThenVat()
    {
        var bookId = Guid.NewGuid();
        var pricing = new PricingPolicy(Guid.NewGuid(), bookId, new Money(100m, Currency.USD), 0m);
        pricing.SetPromo(new Money(80m, Currency.USD), _now.AddDays(-1), _now.AddDays(1), "Sale");

        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(pricing);

        var result = await CreateHandler().Handle(Query(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(80m, item.FinalPrice);
        Assert.NotNull(item.AppliedPromotion);
        Assert.Equal("Sale", item.AppliedPromotion!.Name);
        Assert.Equal(20m, item.AppliedPromotion.DiscountValue);
    }

    [Fact]
    public async Task Handle_BookCurrencyMismatch_ReturnsValidationError()
    {
        var bookId = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(BookFactory.Published(bookId));
        _pricingRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingPolicy(Guid.NewGuid(), bookId, new Money(10m, "EUR")));

        var result = await CreateHandler().Handle(Query(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }
}
