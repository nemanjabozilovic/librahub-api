using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Notifications.Commands.DeleteNotifications;
using LibraHub.Notifications.Domain.Notifications;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Notifications.Commands.DeleteNotifications;

public class DeleteNotificationsHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _userId = Guid.NewGuid();

    private DeleteNotificationsHandler CreateHandler()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));

        return new DeleteNotificationsHandler(_notificationRepository.Object, _currentUser.Object, _unitOfWork.Object);
    }

    private Notification CreateNotification(Guid ownerUserId)
    {
        return new Notification(Guid.NewGuid(), ownerUserId, NotificationType.OrderPaid, "Title", "Message");
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteNotificationsCommand([Guid.NewGuid()]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenNoNotificationIds_ReturnsValidationError()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteNotificationsCommand([]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ReturnsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _notificationRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteNotificationsCommand([Guid.NewGuid()]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenNotificationBelongsToAnotherUser_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        var foreign = CreateNotification(Guid.NewGuid());
        _notificationRepository
            .Setup(r => r.GetByIdAsync(foreign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreign);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteNotificationsCommand([foreign.Id]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_DeletesRange()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        var notification = CreateNotification(_userId);
        _notificationRepository
            .Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteNotificationsCommand([notification.Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _notificationRepository.Verify(
            r => r.DeleteRangeAsync(It.Is<IEnumerable<Notification>>(n => n.Contains(notification)), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
