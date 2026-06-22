using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Consumers;
using LibraHub.Library.Domain.Entitlements;
using LibraHub.Library.Domain.Reading;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Consumers;

public class UserRemovedConsumerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IReadingProgressRepository> _readingProgressRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UserRemovedConsumerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private UserRemovedConsumer CreateConsumer() => new(
        _entitlementRepository.Object,
        _readingProgressRepository.Object,
        _unitOfWork.Object,
        NullLogger<UserRemovedConsumer>.Instance);

    private static UserRemovedV1 Event(Guid userId) => new()
    {
        UserId = userId,
        Reason = "account deletion",
        OccurredAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Handle_NoEntitlements_ReturnsEarly()
    {
        var userId = Guid.NewGuid();
        _entitlementRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Entitlement>());

        await CreateConsumer().HandleAsync(Event(userId), CancellationToken.None);

        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _readingProgressRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RevokesOnlyActiveEntitlementsAndDeletesProgress()
    {
        var userId = Guid.NewGuid();
        var active = new Entitlement(Guid.NewGuid(), userId, Guid.NewGuid(), EntitlementSource.Purchase);
        var revoked = new Entitlement(Guid.NewGuid(), userId, Guid.NewGuid(), EntitlementSource.Purchase);
        revoked.Revoke("earlier");

        _entitlementRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { active, revoked });

        var progress = new ReadingProgress(Guid.NewGuid(), userId, Guid.NewGuid());
        _readingProgressRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadingProgress> { progress });

        await CreateConsumer().HandleAsync(Event(userId), CancellationToken.None);

        Assert.False(active.IsActive);
        _entitlementRepository.Verify(r => r.UpdateAsync(active, It.IsAny<CancellationToken>()), Times.Once);
        _entitlementRepository.Verify(r => r.UpdateAsync(revoked, It.IsAny<CancellationToken>()), Times.Never);
        _readingProgressRepository.Verify(r => r.DeleteAsync(progress, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HasEntitlementsButNoProgress_DoesNotOpenProgressTransaction()
    {
        var userId = Guid.NewGuid();
        var active = new Entitlement(Guid.NewGuid(), userId, Guid.NewGuid(), EntitlementSource.Purchase);
        _entitlementRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entitlement> { active });
        _readingProgressRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadingProgress>());

        await CreateConsumer().HandleAsync(Event(userId), CancellationToken.None);

        _readingProgressRepository.Verify(r => r.DeleteAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
