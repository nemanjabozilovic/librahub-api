namespace LibraHub.Orders.Application.Abstractions;

public record OrderStatisticsResult
{
    public int Total { get; init; }
    public int Paid { get; init; }
    public int Pending { get; init; }
    public int Cancelled { get; init; }
    public int Refunded { get; init; }
    public PeriodStatisticsData Last30Days { get; init; } = new();
    public PeriodStatisticsData Last7Days { get; init; } = new();
    public decimal TotalRevenue { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public class PeriodStatisticsData
{
    public int Count { get; init; }
    public decimal Revenue { get; init; }
}