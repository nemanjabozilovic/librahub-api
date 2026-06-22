using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Consumers;
using LibraHub.Notifications.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Consumers;

public class UserRemovedConsumerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IUserNotificationSettingsRepository> _settingsRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<UserRemovedConsumer>> _logger = new();

    private readonly Guid _userId = Guid.NewGuid();

    private UserRemovedConsumer CreateConsumer()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));

        return new UserRemovedConsumer(
            _notificationRepository.Object,
            _settingsRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private UserRemovedV1 Event() => new()
    {
        UserId = _userId,
        Reason = "Account deletion"
    };

    [Fact]
    public async Task HandleAsync_DeletesAllNotificationsAndSettings()
    {
        var notification = new Notification(Guid.NewGuid(), _userId, NotificationType.OrderPaid, "Title", "Message");
        _notificationRepository
            .Setup(r => r.GetAllByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([notification]);
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationRepository.Verify(r => r.DeleteAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        _settingsRepository.Verify(r => r.DeleteAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoNotifications_StillDeletesSettings()
    {
        _notificationRepository
            .Setup(r => r.GetAllByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationRepository.Verify(r => r.DeleteAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        _settingsRepository.Verify(r => r.DeleteAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
