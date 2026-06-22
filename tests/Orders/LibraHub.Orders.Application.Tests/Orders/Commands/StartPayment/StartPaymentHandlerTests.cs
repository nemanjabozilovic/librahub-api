using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Orders.Commands.StartPayment;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Orders.Commands.StartPayment;

public class StartPaymentHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    public StartPaymentHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
    }

    private StartPaymentHandler CreateHandler() => new(
        _orderRepository.Object,
        _paymentRepository.Object,
        _paymentGateway.Object,
        _outboxWriter.Object,
        _currentUser.Object,
        _clock.Object);

    private StartPaymentCommand Command(string provider = "Mock") => new() { OrderId = _orderId, Provider = provider };

    private Order CreateOrder()
    {
        var item = new OrderItem(Guid.NewGuid(), _orderId, Guid.NewGuid(), "Book",
            new Money(10m, "EUR"), new Money(10m, "EUR"), 20m, new Money(2m, "EUR"));
        return new Order(_orderId, _userId, new List<OrderItem> { item },
            new Money(10m, "EUR"), new Money(2m, "EUR"), new Money(12m, "EUR"));
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
    public async Task Handle_OrderNotInCreatedStatus_ReturnsValidationError()
    {
        var order = CreateOrder();
        order.StartPayment();
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidProvider_ReturnsValidationError()
    {
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder());

        var result = await CreateHandler().Handle(Command("NotAProvider"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_GatewayFails_ReturnsInternalError()
    {
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder());
        _paymentGateway
            .Setup(g => g.InitiatePaymentAsync(It.IsAny<Guid>(), It.IsAny<Money>(), It.IsAny<PaymentProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentResult.Failed("declined"));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INTERNAL_ERROR", result.Error!.Code);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Success_TransitionsToPaymentPending_PersistsPayment_AndWritesEvent()
    {
        var order = CreateOrder();
        _orderRepository
            .Setup(r => r.GetByIdAndUserIdAsync(_orderId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _paymentGateway
            .Setup(g => g.InitiatePaymentAsync(It.IsAny<Guid>(), It.IsAny<Money>(), It.IsAny<PaymentProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentResult.Succeeded("ref-123"));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ref-123", result.Value.ProviderReference);
        Assert.Equal(OrderStatus.PaymentPending, order.Status);

        _paymentRepository.Verify(r => r.AddAsync(
            It.Is<Payment>(p => p.ProviderReference == "ref-123" && p.Status == PaymentStatus.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.IsAny<PaymentInitiatedV1>(),
            Contracts.Common.EventTypes.PaymentInitiated,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
