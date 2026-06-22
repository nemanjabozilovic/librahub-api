using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Inbox;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Library.V1;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Consumers;
using LibraHub.Library.Application.Entitlements;
using LibraHub.Library.Domain.Entitlements;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Consumers;

public class OrderPaidConsumerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IInboxRepository> _inboxRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    public OrderPaidConsumerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
        _clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc));
    }

    private OrderPaidConsumer CreateConsumer() => new(
        new EntitlementGrantService(_entitlementRepository.Object),
        _outboxWriter.Object,
        _inboxRepository.Object,
        _unitOfWork.Object,
        _clock.Object,
        NullLogger<OrderPaidConsumer>.Instance);

    private static OrderPaidV1 Event(Guid orderId, Guid userId, params Guid[] bookIds) => new()
    {
        OrderId = orderId,
        UserId = userId,
        PaidAt = DateTimeOffset.UtcNow,
        Items = bookIds.Select(b => new OrderItemDto { BookId = b, BookTitle = "Clean Code" }).ToList()
    };

    [Fact]
    public async Task Handle_NewItem_CreatesEntitlementAndPublishesGranted()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _inboxRepository.Setup(i => i.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _entitlementRepository.Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>())).ReturnsAsync((Entitlement?)null);

        await CreateConsumer().HandleAsync(Event(orderId, userId, bookId), CancellationToken.None);

        _entitlementRepository.Verify(r => r.AddAsync(
            It.Is<Entitlement>(e => e.UserId == userId && e.BookId == bookId && e.Source == EntitlementSource.Purchase && e.OrderId == orderId),
            It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<EntitlementGrantedV1>(e => e.BookId == bookId && e.BookTitle == "Clean Code"),
            EventTypes.EntitlementGranted, It.IsAny<CancellationToken>()), Times.Once);
        _inboxRepository.Verify(i => i.MarkAsProcessedAsync($"OrderPaid_{orderId}", EventTypes.OrderPaid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RevokedExisting_ReactivatesAndPublishesGranted()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var existing = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        existing.Revoke("prev");
        _inboxRepository.Setup(i => i.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _entitlementRepository.Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await CreateConsumer().HandleAsync(Event(orderId, userId, bookId), CancellationToken.None);

        Assert.True(existing.IsActive);
        _entitlementRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _entitlementRepository.Verify(r => r.AddAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementGrantedV1>(), EventTypes.EntitlementGranted, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveExisting_SkipsButStillPublishesGranted()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var existing = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        _inboxRepository.Setup(i => i.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _entitlementRepository.Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await CreateConsumer().HandleAsync(Event(orderId, userId, bookId), CancellationToken.None);

        _entitlementRepository.Verify(r => r.AddAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _entitlementRepository.Verify(r => r.UpdateAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementGrantedV1>(), EventTypes.EntitlementGranted, It.IsAny<CancellationToken>()), Times.Once);
        _inboxRepository.Verify(i => i.MarkAsProcessedAsync(It.IsAny<string>(), EventTypes.OrderPaid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyProcessed_SkipsProcessing()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _inboxRepository.Setup(i => i.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await CreateConsumer().HandleAsync(Event(orderId, userId, bookId), CancellationToken.None);

        _entitlementRepository.Verify(r => r.GetByUserAndBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _entitlementRepository.Verify(r => r.AddAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _inboxRepository.Verify(i => i.MarkAsProcessedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyItems_MarksProcessedWithoutWork()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = new OrderPaidV1 { OrderId = orderId, UserId = userId, Items = new List<OrderItemDto>() };
        _inboxRepository.Setup(i => i.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateConsumer().HandleAsync(evt, CancellationToken.None);

        _entitlementRepository.Verify(r => r.GetByUserAndBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _inboxRepository.Verify(i => i.MarkAsProcessedAsync($"OrderPaid_{orderId}", EventTypes.OrderPaid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonRetryableFailure_InitiatesAutomaticRefund()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _inboxRepository.Setup(i => i.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _entitlementRepository
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("non-retryable"));

        await CreateConsumer().HandleAsync(Event(orderId, userId, bookId), CancellationToken.None);

        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<OrderRefundedV1>(e => e.OrderId == orderId && e.UserId == userId && e.RefundedBy == Guid.Empty),
            EventTypes.OrderRefunded,
            It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementGrantedV1>(), EventTypes.EntitlementGranted, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyOrderId_Throws()
    {
        var evt = Event(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(() => CreateConsumer().HandleAsync(evt, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NullEvent_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateConsumer().HandleAsync(null!, CancellationToken.None));
    }
}
