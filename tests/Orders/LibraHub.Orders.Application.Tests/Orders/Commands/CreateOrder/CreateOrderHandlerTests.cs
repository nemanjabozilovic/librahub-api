using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Commands.CreateOrder;
using LibraHub.Orders.Domain.Orders;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Commands.CreateOrder;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICatalogPricingClient> _catalogClient = new();
    private readonly Mock<ILibraryOwnershipClient> _libraryClient = new();
    private readonly Mock<IIdentityClient> _identityClient = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();

    public CreateOrderHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
        _clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);

        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private CreateOrderHandler CreateHandler() => new(
        _orderRepository.Object,
        _catalogClient.Object,
        _libraryClient.Object,
        _identityClient.Object,
        _currentUser.Object,
        _outboxWriter.Object,
        _clock.Object,
        _unitOfWork.Object);

    private CreateOrderCommand Command() => new() { BookIds = new List<Guid> { _bookId } };

    private void SetupActiveVerifiedUser()
    {
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success<UserInfo>(
                new UserInfo { Id = _userId, IsActive = true, IsEmailVerified = true }));
    }

    private void SetupValidPricingQuote(decimal finalPrice = 10m, bool isPublished = true, bool isRemoved = false)
    {
        var quote = new PricingQuote
        {
            Currency = "EUR",
            Items = new List<PricingQuoteItem>
            {
                new()
                {
                    BookId = _bookId,
                    BookTitle = "Test Book",
                    IsPublished = isPublished,
                    IsRemoved = isRemoved,
                    BasePrice = 10m,
                    FinalPrice = finalPrice,
                    VatRate = 20m
                }
            }
        };

        _catalogClient
            .Setup(c => c.GetPricingQuoteAsync(It.IsAny<List<Guid>>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success(quote));
    }

    private void SetupNotOwned()
    {
        _libraryClient
            .Setup(c => c.GetOwnedBookIdsAsync(_userId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success(new List<Guid>()));
    }

    [Fact]
    public async Task Handle_UserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        _catalogClient.Verify(c => c.GetPricingQuoteAsync(It.IsAny<List<Guid>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserInactive_ReturnsValidationError()
    {
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success<UserInfo>(
                new UserInfo { Id = _userId, IsActive = false, IsEmailVerified = true }));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _catalogClient.Verify(c => c.GetPricingQuoteAsync(It.IsAny<List<Guid>>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailNotVerified_ReturnsValidationError()
    {
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success<UserInfo>(
                new UserInfo { Id = _userId, IsActive = true, IsEmailVerified = false }));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_IdentityClientFails_PropagatesError()
    {
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Failure<UserInfo>(BuildingBlocks.Results.Error.NotFound("User")));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PricingQuoteFails_PropagatesError()
    {
        SetupActiveVerifiedUser();
        _catalogClient
            .Setup(c => c.GetPricingQuoteAsync(It.IsAny<List<Guid>>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Failure<PricingQuote>(BuildingBlocks.Results.Error.Unexpected("boom")));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNEXPECTED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PricingQuoteItemCountMismatch_ReturnsNotFound()
    {
        SetupActiveVerifiedUser();
        var quote = new PricingQuote { Currency = "EUR", Items = new List<PricingQuoteItem>() };
        _catalogClient
            .Setup(c => c.GetPricingQuoteAsync(It.IsAny<List<Guid>>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success(quote));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_BookNotPublished_ReturnsValidationError()
    {
        SetupActiveVerifiedUser();
        SetupValidPricingQuote(isPublished: false);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_BookRemoved_ReturnsValidationError()
    {
        SetupActiveVerifiedUser();
        SetupValidPricingQuote(isRemoved: true);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_BookFree_ReturnsValidationError()
    {
        SetupActiveVerifiedUser();
        SetupValidPricingQuote(finalPrice: 0m);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_BookAlreadyOwned_ReturnsValidationError()
    {
        SetupActiveVerifiedUser();
        SetupValidPricingQuote();
        _libraryClient
            .Setup(c => c.GetOwnedBookIdsAsync(_userId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success(new List<Guid> { _bookId }));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OwnershipCheckFails_PropagatesError()
    {
        SetupActiveVerifiedUser();
        SetupValidPricingQuote();
        _libraryClient
            .Setup(c => c.GetOwnedBookIdsAsync(_userId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Failure<List<Guid>>(BuildingBlocks.Results.Error.Unexpected("boom")));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNEXPECTED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsOrder_AndWritesOrderCreatedEvent()
    {
        SetupActiveVerifiedUser();
        SetupValidPricingQuote();
        SetupNotOwned();

        Order? persisted = null;
        _orderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => persisted = o)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(persisted!.Id, result.Value);
        Assert.Equal(OrderStatus.Created, persisted.Status);
        Assert.Equal(_userId, persisted.UserId);
        Assert.Single(persisted.Items);

        _orderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.IsAny<OrderCreatedV1>(),
            Contracts.Common.EventTypes.OrderCreated,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
