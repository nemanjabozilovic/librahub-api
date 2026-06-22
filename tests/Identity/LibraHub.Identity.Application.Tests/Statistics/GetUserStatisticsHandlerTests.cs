using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Statistics.Queries.GetUserStatistics;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Statistics;

public class GetUserStatisticsHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IClock> _clock = new();

    public GetUserStatisticsHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);
    }

    private GetUserStatisticsHandler CreateHandler() => new(
        _userRepository.Object,
        _clock.Object);

    [Fact]
    public async Task Handle_ReturnsMappedStatistics()
    {
        var stats = new UserStatisticsResult
        {
            Total = 10,
            Active = 7,
            Removed = 2,
            Pending = 1,
            NewLast30Days = 5,
            NewLast7Days = 3
        };
        _userRepository
            .Setup(r => r.GetStatisticsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await CreateHandler().Handle(new GetUserStatisticsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Total);
        Assert.Equal(7, result.Value.Active);
        Assert.Equal(2, result.Value.Removed);
        Assert.Equal(1, result.Value.Pending);
        Assert.Equal(5, result.Value.NewLast30Days);
        Assert.Equal(3, result.Value.NewLast7Days);
    }
}
