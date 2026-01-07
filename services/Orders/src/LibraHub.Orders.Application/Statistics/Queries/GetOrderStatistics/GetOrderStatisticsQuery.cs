using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Orders.Application.Statistics.Queries.GetOrderStatistics;

public record GetOrderStatisticsQuery : IRequest<Result<OrderStatisticsDto>>;
