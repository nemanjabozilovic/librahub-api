namespace LibraHub.Gateway.Api.Dtos.Dashboard;

public class DashboardSummaryDto
{
    public UserStatisticsDto? Users { get; init; }
    public BookStatisticsDto? Books { get; init; }
    public OrderStatisticsDto? Orders { get; init; }
    public EntitlementStatisticsDto? Entitlements { get; init; }
    public RevenueDto Revenue { get; init; } = new();
}

