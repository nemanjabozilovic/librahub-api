using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Notifications.Queries.GetMyNotifications;
using LibraHub.Notifications.Domain.Notifications;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private readonly Guid _userId = Guid.NewGuid();

    private GetMyNotificationsHandler CreateHandler()
    {
        return new GetMyNotificationsHandler(_notificationRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsMappedNotificationsAndTotalCount()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        var notification = new Notification(Guid.NewGuid(), _userId, NotificationType.OrderPaid, "Title", "Message");
        _notificationRepository
            .Setup(r => r.GetByUserIdAsync(_userId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([notification]);
        _notificationRepository
            .Setup(r => r.GetTotalCountByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalCount);
        var dto = Assert.Single(result.Value.Notifications);
        Assert.Equal(notification.Id, dto.Id);
        Assert.Equal("OrderPaid", dto.Type);
        Assert.Equal("Unread", dto.Status);
        Assert.Null(dto.ReadAt);
    }
}
