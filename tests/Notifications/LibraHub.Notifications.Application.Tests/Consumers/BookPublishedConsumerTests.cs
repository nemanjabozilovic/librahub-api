using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Consumers;
using LibraHub.Notifications.Domain.Notifications;
using LibraHub.Notifications.Domain.Recipients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Consumers;

public class BookPublishedConsumerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<INotificationSender> _notificationSender = new();
    private readonly Mock<IUserNotificationSettingsRepository> _settingsRepository = new();
    private readonly Mock<IIdentityClient> _identityClient = new();
    private readonly Mock<IInboxRepository> _inboxRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<BookPublishedConsumer>> _logger = new();

    private readonly Guid _bookId = Guid.NewGuid();

    private BookPublishedConsumer CreateConsumer()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));

        return new BookPublishedConsumer(
            _notificationRepository.Object,
            _notificationSender.Object,
            _settingsRepository.Object,
            _identityClient.Object,
            _inboxRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private BookPublishedV1 Event() => new()
    {
        BookId = _bookId,
        Title = "Clean Code",
        Authors = "Robert Martin"
    };

    private UserNotificationSettings EmailRecipient(Guid userId)
    {
        return new UserNotificationSettings(userId, "recipient@example.com", isActive: true, isStaff: false, emailEnabled: true, inAppEnabled: true);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyProcessed_Skips()
    {
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _settingsRepository.Verify(r => r.GetActiveNonStaffUserIdsWithInAppEnabledAsync(It.IsAny<CancellationToken>()), Times.Never);
        _inboxRepository.Verify(r => r.MarkAsProcessedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEligibleInAppUsers_CreatesRangeAndSendsInApp()
    {
        var userId = Guid.NewGuid();
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetActiveNonStaffUserIdsWithInAppEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync([userId]);
        _settingsRepository.Setup(r => r.GetEmailRecipientsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationRepository.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<Notification>>(n => n.Count() == 1 && n.First().UserId == userId && n.First().Type == NotificationType.BookPublished), It.IsAny<CancellationToken>()),
            Times.Once);
        _notificationSender.Verify(s => s.SendInAppAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _inboxRepository.Verify(r => r.MarkAsProcessedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoEligibleInAppUsers_DoesNotAddRange()
    {
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetActiveNonStaffUserIdsWithInAppEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _settingsRepository.Setup(r => r.GetEmailRecipientsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Notification>>(), It.IsAny<CancellationToken>()), Times.Never);
        _inboxRepository.Verify(r => r.MarkAsProcessedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailRecipientActive_SendsEmail()
    {
        var recipientId = Guid.NewGuid();
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetActiveNonStaffUserIdsWithInAppEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _settingsRepository.Setup(r => r.GetEmailRecipientsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([EmailRecipient(recipientId)]);
        _identityClient
            .Setup(c => c.GetUserInfoAsync(recipientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserInfo { Id = recipientId, Email = "recipient@example.com", IsActive = true }));
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationSender.Verify(
            s => s.SendEmailWithTemplateAsync("recipient@example.com", It.IsAny<string>(), "BOOK_PUBLISHED", It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailRecipientInactive_DoesNotSendEmail()
    {
        var recipientId = Guid.NewGuid();
        _inboxRepository.Setup(r => r.IsProcessedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _settingsRepository.Setup(r => r.GetActiveNonStaffUserIdsWithInAppEnabledAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _settingsRepository.Setup(r => r.GetEmailRecipientsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([EmailRecipient(recipientId)]);
        _identityClient
            .Setup(c => c.GetUserInfoAsync(recipientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserInfo { Id = recipientId, Email = "recipient@example.com", IsActive = false }));
        var consumer = CreateConsumer();

        await consumer.HandleAsync(Event(), CancellationToken.None);

        _notificationSender.Verify(
            s => s.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
