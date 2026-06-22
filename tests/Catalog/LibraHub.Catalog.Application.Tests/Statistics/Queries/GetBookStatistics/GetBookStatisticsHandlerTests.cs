using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Statistics.Queries.GetBookStatistics;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Statistics.Queries.GetBookStatistics;

public class GetBookStatisticsHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IClock> _clock = new();

    private readonly DateTime _now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    public GetBookStatisticsHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(_now);
    }

    private GetBookStatisticsHandler CreateHandler() => new(_bookRepository.Object, _clock.Object);

    [Fact]
    public async Task Handle_ReturnsMappedStatisticsAndQueriesLast30Days()
    {
        _bookRepository
            .Setup(r => r.GetStatisticsAsync(_now.AddDays(-30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookStatisticsResult
            {
                Total = 10,
                Published = 5,
                Draft = 3,
                Unlisted = 2,
                NewLast30Days = 4
            });

        var result = await CreateHandler().Handle(new GetBookStatisticsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Total);
        Assert.Equal(5, result.Value.Published);
        Assert.Equal(3, result.Value.Draft);
        Assert.Equal(2, result.Value.Unlisted);
        Assert.Equal(4, result.Value.NewLast30Days);
        _bookRepository.Verify(r => r.GetStatisticsAsync(_now.AddDays(-30), It.IsAny<CancellationToken>()), Times.Once);
    }
}
