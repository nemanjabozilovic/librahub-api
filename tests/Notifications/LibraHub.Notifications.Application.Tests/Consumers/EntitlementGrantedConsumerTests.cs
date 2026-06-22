using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Library.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Consumers;
using LibraHub.Notifications.Domain.Notifications;
using LibraHub.Notifications.Domain.Recipients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Consumers;

public class EntitlementGrantedConsumerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<INotificationSender> _notificationSender = new();
    private readonly Mock<IUserNotificationSettingsRepository> _settingsRepository = new();
    private readonly Mock<IIdentityClient> _identityClient = new();
    private readonly Mock<IInboxRepository> _inboxRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<EntitlementGrantedConsumer>> _logger = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();

    private EntitlementGrantedConsumer CreateConsumer()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));

        return new EntitlementGrantedConsumer(
            _notificationRepository.Object,
            _notificationSender.Object,
            _settingsRepository.Object,
            _identityClient.Object,
            _inboxRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private EntitlementGrantedV1 Event() => new()
    {
        UserId = _userId,
        BookId = _bookId,
        Source = "Purchase"
    };

    private UserNotificationSettings Settings(bool isActive = true, bool isStaff = false, bool emailEnabled = false, bool inAppEnabled = true)
    {
        return new UserNotificationSettings(_userId, "user@example.com", isActive, isStaff, emailEnabled, inAppEnabled);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyProcessed_Skips()
    {
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _settingsRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenNoSettings_SkipsAndMarksProcessed()
    {
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((UserNotificationSettings?)null);
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _inboxRepository.Verify(r => r.MarkAsProcessedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenInAppEnabled_CreatesNotificationAndSendsInApp()
    {
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(Settings(inAppEnabled: true));
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == NotificationType.EntitlementGranted), It.IsAny<CancellationToken>()), Times.Once);
        _notificationSender.Verify(s => s.SendInAppAsync(_userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailEnabledAndUserActive_SendsEmail()
    {
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(Settings(emailEnabled: true));
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserInfo { Id = _userId, Email = "user@example.com", IsActive = true }));
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationSender.Verify(
            s => s.SendEmailWithTemplateAsync("user@example.com", It.IsAny<string>(), "ENTITLEMENT_GRANTED", It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityClientFails_DoesNotSendEmail()
    {
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(Settings(emailEnabled: true));
        _identityClient
            .Setup(c => c.GetUserInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserInfo>(Error.NotFound("User")));
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationSender.Verify(
            s => s.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
