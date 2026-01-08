using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using MediatR;

namespace LibraHub.Identity.Application.Statistics.Queries.GetUserStatistics;

public class GetUserStatisticsHandler(
    IUserRepository userRepository,
    IClock clock) : IRequestHandler<GetUserStatisticsQuery, Result<UserStatisticsDto>>
{
    public async Task<Result<UserStatisticsDto>> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var last30Days = now.AddDays(-30);
        var last7Days = now.AddDays(-7);

        var statistics = await userRepository.GetStatisticsAsync(last30Days, last7Days, cancellationToken);

        var response = new UserStatisticsDto
        {
            Total = statistics.Total,
            Active = statistics.Active,
            Removed = statistics.Removed,
            Pending = statistics.Pending,
            NewLast30Days = statistics.NewLast30Days,
            NewLast7Days = statistics.NewLast7Days
        };

        return Result.Success(response);
    }
}
