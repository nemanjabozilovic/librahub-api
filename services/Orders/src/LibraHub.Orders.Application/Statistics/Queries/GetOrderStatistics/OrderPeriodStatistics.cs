namespace LibraHub.Orders.Application.Statistics.Queries.GetOrderStatistics;

public class OrderPeriodStatistics
{
    public int Count { get; init; }
    public decimal Revenue { get; init; }
    public string Currency { get; init; } = string.Empty;
}
