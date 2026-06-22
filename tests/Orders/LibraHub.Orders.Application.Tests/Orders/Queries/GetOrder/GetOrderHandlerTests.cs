using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Queries.GetOrder;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Queries.GetOrder;

public class GetOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IIdentityClient> _identityClient = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    private GetOrderHandler CreateHandler() => new(
        _orderRepository.Object,
        _paymentRepository.Object,
        _identityClient.Object,
        _currentUser.Object);

    private GetOrderQuery Query() => new() { OrderId = _orderId };

    private Order CreateOrder()
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        return new Order(_orderId, _userId, new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
    }

    [Fact]
    public async Task Handle_NonAdminNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(Query(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await CreateHandler().Handle(Query(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NonAdmin_LoadsOrderScopedToUser_ReturnsDto()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder());
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Success<UserInfo>(
                new UserInfo { Id = _userId, Email = "u@x.com", DisplayName = "User" }));

        var result = await CreateHandler().Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_orderId, result.Value.Id);
        Assert.Equal("User", result.Value.UserDisplayName);
        Assert.Null(result.Value.Payment);
        _orderRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Admin_LoadsOrderById_IncludesPayment()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        var order = CreateOrder();
        order.StartPayment();
        var payment = new Payment(Guid.NewGuid(), _orderId, PaymentProvider.Mock, new Money(12m, "EUR"));
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Results.Result.Failure<UserInfo>(BuildingBlocks.Results.Error.NotFound("User")));

        var result = await CreateHandler().Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Payment);
        Assert.Null(result.Value.UserDisplayName);
        _orderRepository.Verify(r => r.GetByIdAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
