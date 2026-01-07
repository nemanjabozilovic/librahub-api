namespace LibraHub.Orders.Application.Statistics.Queries.GetOrderStatistics;

public class OrderStatisticsDto
{
    public int Total { get; init; }
    public int Paid { get; init; }
    public int Pending { get; init; }
    public int Cancelled { get; init; }
    public int Refunded { get; init; }
    public PeriodStatistics Last30Days { get; init; } = new();
    public PeriodStatistics Last7Days { get; init; } = new();
    public decimal TotalRevenue { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public class PeriodStatistics
{
    public int Count { get; init; }
    public decimal Revenue { get; init; }
}
