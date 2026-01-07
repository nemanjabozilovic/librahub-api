using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using MediatR;

namespace LibraHub.Library.Application.Statistics.Queries.GetEntitlementStatistics;

public class GetEntitlementStatisticsHandler(
    IEntitlementRepository entitlementRepository,
    IClock clock) : IRequestHandler<GetEntitlementStatisticsQuery, Result<EntitlementStatisticsDto>>
{
    public async Task<Result<EntitlementStatisticsDto>> Handle(GetEntitlementStatisticsQuery request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var last30Days = now.AddDays(-30);

        var statistics = await entitlementRepository.GetStatisticsAsync(last30Days, cancellationToken);

        var response = new EntitlementStatisticsDto
        {
            Total = statistics.Total,
            Active = statistics.Active,
            Revoked = statistics.Revoked,
            GrantedLast30Days = statistics.GrantedLast30Days
        };

        return Result.Success(response);
    }
}
