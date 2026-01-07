using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using MediatR;

namespace LibraHub.Orders.Application.Statistics.Queries.GetOrderStatistics;

public class GetOrderStatisticsHandler(
    IOrderRepository orderRepository,
    IClock clock) : IRequestHandler<GetOrderStatisticsQuery, Result<OrderStatisticsDto>>
{
    public async Task<Result<OrderStatisticsDto>> Handle(GetOrderStatisticsQuery request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var last30Days = now.AddDays(-30);
        var last7Days = now.AddDays(-7);

        var statistics = await orderRepository.GetStatisticsAsync(last30Days, last7Days, now, cancellationToken);

        var response = new OrderStatisticsDto
        {
            Total = statistics.Total,
            Paid = statistics.Paid,
            Pending = statistics.Pending,
            Cancelled = statistics.Cancelled,
            Refunded = statistics.Refunded,
            Last30Days = new PeriodStatistics
            {
                Count = statistics.Last30Days.Count,
                Revenue = statistics.Last30Days.Revenue
            },
            Last7Days = new PeriodStatistics
            {
                Count = statistics.Last7Days.Count,
                Revenue = statistics.Last7Days.Revenue
            },
            TotalRevenue = statistics.TotalRevenue,
            Currency = statistics.Currency
        };

        return Result.Success(response);
    }
}
