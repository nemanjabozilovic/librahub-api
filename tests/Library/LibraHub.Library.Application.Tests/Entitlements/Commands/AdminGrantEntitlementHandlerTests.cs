using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Library.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Entitlements;
using LibraHub.Library.Application.Entitlements.Commands.AdminGrantEntitlement;
using LibraHub.Library.Domain.Entitlements;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Entitlements.Commands;

public class AdminGrantEntitlementHandlerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IBookSnapshotStore> _bookSnapshotStore = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();

    private AdminGrantEntitlementHandler CreateHandler() =>
        new(new EntitlementGrantService(_entitlementRepository.Object), _bookSnapshotStore.Object, _outboxWriter.Object);

    private static Entitlement CreateRevoked(Guid userId, Guid bookId)
    {
        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        entitlement.Revoke("test");
        return entitlement;
    }

    [Fact]
    public async Task Handle_AlreadyActive_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var existing = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        _entitlementRepository
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(
            new AdminGrantEntitlementCommand { UserId = userId, BookId = bookId }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _entitlementRepository.Verify(r => r.AddAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _entitlementRepository.Verify(r => r.UpdateAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementGrantedV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Revoked_ReactivatesAndPublishesEvent()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var existing = CreateRevoked(userId, bookId);
        _entitlementRepository
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(
            new AdminGrantEntitlementCommand { UserId = userId, BookId = bookId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value);
        Assert.True(existing.IsActive);
        _entitlementRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _entitlementRepository.Verify(r => r.AddAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementGrantedV1>(), EventTypes.EntitlementGranted, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoExisting_CreatesEntitlementAndPublishesEvent()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _entitlementRepository
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entitlement?)null);

        Entitlement? added = null;
        _entitlementRepository
            .Setup(r => r.AddAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()))
            .Callback<Entitlement, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(
            new AdminGrantEntitlementCommand { UserId = userId, BookId = bookId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(result.Value, added!.Id);
        Assert.Equal(EntitlementSource.AdminGrant, added.Source);
        Assert.True(added.IsActive);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementGrantedV1>(), EventTypes.EntitlementGranted, It.IsAny<CancellationToken>()), Times.Once);
    }
}
