using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Commands.RefundOrder;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using LibraHub.Orders.Domain.Refunds;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Commands.RefundOrder;

public class RefundOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IRefundRepository> _refundRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    public RefundOrderHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_adminId);
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private RefundOrderHandler CreateHandler() => new(
        _orderRepository.Object,
        _paymentRepository.Object,
        _refundRepository.Object,
        _outboxWriter.Object,
        _currentUser.Object,
        _clock.Object,
        _unitOfWork.Object);

    private RefundOrderCommand Command() => new() { OrderId = _orderId, Reason = "defective" };

    private Order CreatePaidOrder()
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        var order = new Order(_orderId, Guid.NewGuid(), new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
        order.StartPayment();
        order.MarkAsPaid();
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
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_OrderNotPaid_ReturnsValidationError()
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        var order = new Order(_orderId, Guid.NewGuid(), new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ReturnsNotFound()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePaidOrder());
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_Success_MarksRefunded_PersistsRefund_AndWritesEvent()
    {
        var order = CreatePaidOrder();
        var payment = new Payment(Guid.NewGuid(), order.Id, PaymentProvider.Mock, new Money(12m, "EUR"));
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Refunded, order.Status);
        _refundRepository.Verify(r => r.AddAsync(
            It.Is<Refund>(rf => rf.RefundedBy == _adminId && rf.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.IsAny<OrderRefundedV1>(),
            Contracts.Common.EventTypes.OrderRefunded,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
