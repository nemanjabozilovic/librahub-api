using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Application.Statistics.Queries.GetOrderStatistics;
using Moq;
using Xunit;

namespace LibraHub.Orders.Application.Tests.Statistics.Queries.GetOrderStatistics;

public class GetOrderStatisticsHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IClock> _clock = new();

    private GetOrderStatisticsHandler CreateHandler() => new(_orderRepository.Object, _clock.Object);

    [Fact]
    public async Task Handle_MapsStatisticsResultToDto()
    {
        var now = new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc);
        _clock.SetupGet(c => c.UtcNow).Returns(now);

        var stats = new OrderStatisticsResult
        {
            Total = 10,
            Paid = 6,
            Pending = 2,
            Cancelled = 1,
            Refunded = 1,
            Last30Days = new PeriodStatisticsData { Count = 5, Revenue = 250m },
            Last7Days = new PeriodStatisticsData { Count = 2, Revenue = 100m },
            TotalRevenue = 1000m,
            Currency = "EUR"
        };

        _orderRepository
            .Setup(r => r.GetStatisticsAsync(
                now.AddDays(-30), now.AddDays(-7), now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await CreateHandler().Handle(new GetOrderStatisticsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Total);
        Assert.Equal(6, result.Value.Paid);
        Assert.Equal(5, result.Value.Last30Days.Count);
        Assert.Equal(250m, result.Value.Last30Days.Revenue);
        Assert.Equal(2, result.Value.Last7Days.Count);
        Assert.Equal(1000m, result.Value.TotalRevenue);
        Assert.Equal("EUR", result.Value.Currency);
    }
}
