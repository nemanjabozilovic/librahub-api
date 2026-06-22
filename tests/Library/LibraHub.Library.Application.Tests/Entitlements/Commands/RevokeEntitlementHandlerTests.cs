using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Library.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Entitlements.Commands.RevokeEntitlement;
using LibraHub.Library.Domain.Entitlements;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Entitlements.Commands;

public class RevokeEntitlementHandlerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();

    private RevokeEntitlementHandler CreateHandler() =>
        new(_entitlementRepository.Object, _outboxWriter.Object);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _entitlementRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entitlement?)null);

        var result = await CreateHandler().Handle(
            new RevokeEntitlementCommand { EntitlementId = id }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementRevokedV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRevoked_ReturnsValidationError()
    {
        var entitlement = new Entitlement(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EntitlementSource.Purchase);
        entitlement.Revoke("already");
        _entitlementRepository
            .Setup(r => r.GetByIdAsync(entitlement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlement);

        var result = await CreateHandler().Handle(
            new RevokeEntitlementCommand { EntitlementId = entitlement.Id }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _entitlementRepository.Verify(r => r.UpdateAsync(It.IsAny<Entitlement>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<EntitlementRevokedV1>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Active_RevokesAndPublishesEvent()
    {
        var entitlement = new Entitlement(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EntitlementSource.Purchase);
        _entitlementRepository
            .Setup(r => r.GetByIdAsync(entitlement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlement);

        var result = await CreateHandler().Handle(
            new RevokeEntitlementCommand { EntitlementId = entitlement.Id, Reason = "fraud" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(entitlement.IsActive);
        _entitlementRepository.Verify(r => r.UpdateAsync(entitlement, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<EntitlementRevokedV1>(e => e.Reason == "fraud"),
            EventTypes.EntitlementRevoked,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveWithoutReason_UsesDefaultReason()
    {
        var entitlement = new Entitlement(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), EntitlementSource.Purchase);
        _entitlementRepository
            .Setup(r => r.GetByIdAsync(entitlement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlement);

        var result = await CreateHandler().Handle(
            new RevokeEntitlementCommand { EntitlementId = entitlement.Id, Reason = null }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<EntitlementRevokedV1>(e => e.Reason == "Manual revocation"),
            EventTypes.EntitlementRevoked,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
