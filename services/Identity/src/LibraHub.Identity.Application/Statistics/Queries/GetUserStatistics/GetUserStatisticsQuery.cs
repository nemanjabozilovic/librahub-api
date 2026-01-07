using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Statistics.Queries.GetUserStatistics;

public record GetUserStatisticsQuery : IRequest<Result<UserStatisticsDto>>;
