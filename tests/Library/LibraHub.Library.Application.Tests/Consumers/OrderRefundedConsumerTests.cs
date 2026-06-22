using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Library.V1;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Consumers;
using LibraHub.Library.Domain.Entitlements;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Consumers;

public class OrderRefundedConsumerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public OrderRefundedConsumerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private OrderRefundedConsumer CreateConsumer() => new(
        _entitlementRepository.Object,
        _outboxWriter.Object,
        _unitOfWork.Object,
        NullLogger<OrderRefundedConsumer>.Instance);

    private static OrderRefundedV1 Event(Guid orderId, Guid userId) => new()
    {
        OrderId = orderId,
        UserId = userId,
        Reason = "customer request",
        RefundedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_RevokesOnlyActiveEntitlementsForOrder()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var matchActive = new Entitlement(Guid.NewGuid(), userId, Guid.NewGuid(), EntitlementSource.Purchase, orderId);
        var matchRevoked = new Entitlement(Guid.NewGuid(), userId, Guid.NewGuid(), EntitlementSource.Purchase, orderId);
        matchRevoked.Revoke("earlier");
        var otherOrder = new Entitlement(Guid.NewGuid(), userId, Guid.NewGuid(), EntitlementSource.Purchase, Guid.NewGuid());

        _entitlementRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { matchActive, matchRevoked, otherOrder });

        await CreateConsumer().HandleAsync(Event(orderId, userId), CancellationToken.None);

        Assert.False(matchActive.IsActive);
        _entitlementRepository.Verify(r => r.UpdateAsync(matchActive, It.IsAny<CancellationToken>()), Times.Once);
        _entitlementRepository.Verify(r => r.UpdateAsync(matchRevoked, It.IsAny<CancellationToken>()), Times.Never);
        _entitlementRepository.Verify(r => r.UpdateAsync(otherOrder, It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementRevokedV1>(), EventTypes.EntitlementRevoked, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoMatchingEntitlements_DoesNothing()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _entitlementRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement>());

        await CreateConsumer().HandleAsync(Event(orderId, userId), CancellationToken.None);

        _entitlementRepository.Verify(r => r.UpdateAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementRevokedV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
