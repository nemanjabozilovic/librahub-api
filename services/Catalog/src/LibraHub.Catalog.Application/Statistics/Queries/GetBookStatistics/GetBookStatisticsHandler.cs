using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using MediatR;

namespace LibraHub.Catalog.Application.Statistics.Queries.GetBookStatistics;

public class GetBookStatisticsHandler(
    IBookRepository bookRepository,
    IClock clock) : IRequestHandler<GetBookStatisticsQuery, Result<BookStatisticsDto>>
{
    public async Task<Result<BookStatisticsDto>> Handle(GetBookStatisticsQuery request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var last30Days = now.AddDays(-30);

        var statistics = await bookRepository.GetStatisticsAsync(last30Days, cancellationToken);

        var response = new BookStatisticsDto
        {
            Total = statistics.Total,
            Published = statistics.Published,
            Draft = statistics.Draft,
            Unlisted = statistics.Unlisted,
            NewLast30Days = statistics.NewLast30Days
        };

        return Result.Success(response);
    }
}
