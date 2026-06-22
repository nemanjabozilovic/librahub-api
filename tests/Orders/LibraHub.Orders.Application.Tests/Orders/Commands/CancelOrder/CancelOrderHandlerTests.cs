using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Commands.CancelOrder;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Commands.CancelOrder;

public class CancelOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    public CancelOrderHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
    }

    private CancelOrderHandler CreateHandler() => new(
        _orderRepository.Object,
        _paymentRepository.Object,
        _outboxWriter.Object,
        _currentUser.Object,
        _clock.Object);

    private CancelOrderCommand Command() => new() { OrderId = _orderId, Reason = "changed mind" };

    private Order CreateOrder(bool startPayment = false)
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        var order = new Order(_orderId, _userId, new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
        if (startPayment)
        {
            order.StartPayment();
        }

        return order;
    }

    [Fact]
    public async Task Handle_UserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_OrderCannotBeCancelled_ReturnsValidationError()
    {
        var order = CreateOrder(startPayment: true);
        order.MarkAsPaid();
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_CreatedOrderWithoutPayment_Cancels_AndWritesEvent()
    {
        var order = CreateOrder();
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.IsAny<OrderCancelledV1>(),
            Contracts.Common.EventTypes.OrderCancelled,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PendingOrderWithPendingPayment_CancelsBoth()
    {
        var order = CreateOrder(startPayment: true);
        var payment = new Payment(Guid.NewGuid(), order.Id, PaymentProvider.Mock, new Money(12m, "EUR"));
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        _paymentRepository.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
    }
}
