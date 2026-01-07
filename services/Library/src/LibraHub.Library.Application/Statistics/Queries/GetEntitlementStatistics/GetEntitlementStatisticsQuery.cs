using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Library.Application.Statistics.Queries.GetEntitlementStatistics;

public record GetEntitlementStatisticsQuery : IRequest<Result<EntitlementStatisticsDto>>;
