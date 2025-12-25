namespace LibraHub.Gateway.Api.Dtos.Dashboard;

public class RevenueDto
{
    public decimal Total { get; init; }
    public decimal Last30Days { get; init; }
    public decimal Last7Days { get; init; }
    public string Currency { get; init; } = string.Empty;
}

