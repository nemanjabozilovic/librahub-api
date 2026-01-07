namespace LibraHub.Identity.Application.Statistics.Queries.GetUserStatistics;

public class UserStatisticsDto
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Disabled { get; init; }
    public int Pending { get; init; }
    public int NewLast30Days { get; init; }
    public int NewLast7Days { get; init; }
}
