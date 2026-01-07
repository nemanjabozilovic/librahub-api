using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Statistics.Queries.GetBookStatistics;

public record GetBookStatisticsQuery : IRequest<Result<BookStatisticsDto>>;
