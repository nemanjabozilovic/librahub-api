namespace LibraHub.Gateway.Api.Dtos.Dashboard;

public class EntitlementStatisticsDto
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Revoked { get; init; }
    public int GrantedLast30Days { get; init; }
}

