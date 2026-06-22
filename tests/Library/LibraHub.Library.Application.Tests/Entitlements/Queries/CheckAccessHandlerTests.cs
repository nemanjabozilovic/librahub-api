using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Entitlements.Queries.CheckAccess;
using LibraHub.Library.Domain.Entitlements;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Entitlements.Queries;

public class CheckAccessHandlerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();

    private CheckAccessHandler CreateHandler() => new(_entitlementRepository.Object);

    [Fact]
    public async Task Handle_NoEntitlement_ReturnsNoneStatus()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _entitlementRepository
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entitlement?)null);

        var result = await CreateHandler().Handle(
            new CheckAccessQuery { UserId = userId, BookId = bookId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasAccess);
        Assert.Equal("None", result.Value.Status);
    }

    [Fact]
    public async Task Handle_ActiveEntitlement_ReturnsActiveStatus()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        _entitlementRepository
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlement);

        var result = await CreateHandler().Handle(
            new CheckAccessQuery { UserId = userId, BookId = bookId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasAccess);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task Handle_RevokedEntitlement_ReturnsRevokedStatus()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, EntitlementSource.Purchase);
        entitlement.Revoke("test");
        _entitlementRepository
            .Setup(r => r.GetByUserAndBookAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlement);

        var result = await CreateHandler().Handle(
            new CheckAccessQuery { UserId = userId, BookId = bookId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasAccess);
        Assert.Equal("Revoked", result.Value.Status);
    }
}
