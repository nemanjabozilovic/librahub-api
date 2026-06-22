using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Statistics.Queries.GetEntitlementStatistics;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Statistics.Queries;

public class GetEntitlementStatisticsHandlerTests
{
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<IClock> _clock = new();

    private GetEntitlementStatisticsHandler CreateHandler() =>
        new(_entitlementRepository.Object, _clock.Object);

    [Fact]
    public async Task Handle_ReturnsMappedStatistics_UsingClockLast30Days()
    {
        var now = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(c => c.UtcNow).Returns(now);

        _entitlementRepository
            .Setup(r => r.GetStatisticsAsync(now.AddDays(-30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntitlementStatisticsResult
            {
                Total = 100,
                Active = 80,
                Revoked = 20,
                GrantedLast30Days = 15
            });

        var result = await CreateHandler().Handle(new GetEntitlementStatisticsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.Total);
        Assert.Equal(80, result.Value.Active);
        Assert.Equal(20, result.Value.Revoked);
        Assert.Equal(15, result.Value.GrantedLast30Days);
        _entitlementRepository.Verify(r => r.GetStatisticsAsync(now.AddDays(-30), It.IsAny<CancellationToken>()), Times.Once);
    }
}
