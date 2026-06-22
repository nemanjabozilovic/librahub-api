using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Commands.CapturePayment;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Commands.CapturePayment;

public class CapturePaymentHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _paymentId = Guid.NewGuid();
    private const string ProviderRef = "ref-123";

    public CapturePaymentHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private CapturePaymentHandler CreateHandler() => new(
        _orderRepository.Object,
        _paymentRepository.Object,
        _paymentGateway.Object,
        _outboxWriter.Object,
        _currentUser.Object,
        _clock.Object,
        _unitOfWork.Object);

    private CapturePaymentCommand Command() => new() { OrderId = _orderId, PaymentId = _paymentId, ProviderReference = ProviderRef };

    private Order CreatePendingOrder()
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        var order = new Order(_orderId, _userId, new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
        order.StartPayment();
        return order;
    }

    private Payment CreatePayment(string providerRef = ProviderRef)
    {
        var payment = new Payment(_paymentId, _orderId, PaymentProvider.Mock, new Money(12m, "EUR"));
        payment.SetProviderReference(providerRef);
        return payment;
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
    public async Task Handle_OrderNotInPaymentPending_ReturnsValidationError()
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        var order = new Order(_orderId, _userId, new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ReturnsNotFound()
    {
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePendingOrder());
        _paymentRepository
            .Setup(r => r.GetByIdAsync(_paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PaymentBelongsToDifferentOrder_ReturnsValidationError()
    {
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePendingOrder());
        var payment = new Payment(_paymentId, Guid.NewGuid(), PaymentProvider.Mock, new Money(12m, "EUR"));
        payment.SetProviderReference(ProviderRef);
        _paymentRepository
            .Setup(r => r.GetByIdAsync(_paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ProviderReferenceMismatch_ReturnsValidationError()
    {
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePendingOrder());
        _paymentRepository
            .Setup(r => r.GetByIdAsync(_paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePayment("different-ref"));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _paymentGateway.Verify(g => g.CapturePaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GatewayFails_CancelsOrder_MarksPaymentFailed_ReturnsInternalError()
    {
        var order = CreatePendingOrder();
        var payment = CreatePayment();
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentRepository
            .Setup(r => r.GetByIdAsync(_paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _paymentGateway
            .Setup(g => g.CapturePaymentAsync(ProviderRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentResult.Failed("declined"));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INTERNAL_ERROR", result.Error!.Code);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<OrderPaidV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Success_MarksPaid_CompletesPayment_AndWritesOrderPaidEvent()
    {
        var order = CreatePendingOrder();
        var payment = CreatePayment();
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentRepository
            .Setup(r => r.GetByIdAsync(_paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _paymentGateway
            .Setup(g => g.CapturePaymentAsync(ProviderRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentResult.Succeeded(ProviderRef));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.IsAny<OrderPaidV1>(),
            Contracts.Common.EventTypes.OrderPaid,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
