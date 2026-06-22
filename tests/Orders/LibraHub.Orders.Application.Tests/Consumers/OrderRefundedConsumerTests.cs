using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Consumers;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using LibraHub.Orders.Domain.Refunds;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Consumers;

public class OrderRefundedConsumerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IRefundRepository> _refundRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<OrderRefundedConsumer>> _logger = new();

    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _refundId = Guid.NewGuid();

    public OrderRefundedConsumerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private OrderRefundedConsumer CreateConsumer() => new(
        _orderRepository.Object,
        _paymentRepository.Object,
        _refundRepository.Object,
        _unitOfWork.Object,
        _logger.Object);

    private OrderRefundedV1 Event() => new()
    {
        OrderId = _orderId,
        RefundId = _refundId,
        UserId = Guid.NewGuid(),
        Reason = "defective",
        RefundedBy = Guid.NewGuid(),
        RefundedAt = DateTimeOffset.UtcNow
    };

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
    public async Task HandleAsync_OrderNotFound_SkipsProcessing()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await CreateConsumer().HandleAsync(Event(), CancellationToken.None);

        _refundRepository.Verify(r => r.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OrderNotPaid_SkipsProcessing()
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        var order = new Order(_orderId, Guid.NewGuid(), new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await CreateConsumer().HandleAsync(Event(), CancellationToken.None);

        _refundRepository.Verify(r => r.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PaymentNotFound_SkipsProcessing()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePaidOrder());
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        await CreateConsumer().HandleAsync(Event(), CancellationToken.None);

        _refundRepository.Verify(r => r.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RefundAlreadyExists_SkipsProcessing()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePaidOrder());
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment(Guid.NewGuid(), _orderId, PaymentProvider.Mock, new Money(12m, "EUR")));
        _refundRepository
            .Setup(r => r.GetByIdAsync(_refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Refund(_refundId, _orderId, Guid.NewGuid(), "defective", Guid.NewGuid()));

        await CreateConsumer().HandleAsync(Event(), CancellationToken.None);

        _refundRepository.Verify(r => r.AddAsync(It.IsAny<Refund>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Valid_PersistsRefund_AndMarksOrderRefunded()
    {
        var order = CreatePaidOrder();
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment(Guid.NewGuid(), _orderId, PaymentProvider.Mock, new Money(12m, "EUR")));
        _refundRepository
            .Setup(r => r.GetByIdAsync(_refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        await CreateConsumer().HandleAsync(Event(), CancellationToken.None);

        Assert.Equal(OrderStatus.Refunded, order.Status);
        _refundRepository.Verify(r => r.AddAsync(
            It.Is<Refund>(rf => rf.Id == _refundId && rf.OrderId == _orderId),
            It.IsAny<CancellationToken>()), Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TransactionThrows_PropagatesException()
    {
        _orderRepository
            .Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePaidOrder());
        _paymentRepository
            .Setup(r => r.GetByOrderIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment(Guid.NewGuid(), _orderId, PaymentProvider.Mock, new Money(12m, "EUR")));
        _refundRepository
            .Setup(r => r.GetByIdAsync(_refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateConsumer().HandleAsync(Event(), CancellationToken.None));
    }
}
