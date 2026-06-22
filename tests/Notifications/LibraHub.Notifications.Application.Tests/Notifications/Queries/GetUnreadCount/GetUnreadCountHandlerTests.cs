using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Notifications.Queries.GetUnreadCount;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private readonly Guid _userId = Guid.NewGuid();

    private GetUnreadCountHandler CreateHandler()
    {
        return new GetUnreadCountHandler(_notificationRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUnreadCountQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsUnreadCount()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _notificationRepository
            .Setup(r => r.GetUnreadCountByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUnreadCountQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }
}
