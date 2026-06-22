using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Consumers;
using LibraHub.Notifications.Domain.Recipients;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Consumers;

public class UserNotificationSettingsChangedConsumerTests
{
    private readonly Mock<IUserNotificationSettingsRepository> _settingsRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ILogger<UserNotificationSettingsChangedConsumer>> _logger = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 6, 22, 10, 0, 0, DateTimeKind.Utc);

    private UserNotificationSettingsChangedConsumer CreateConsumer()
    {
        _clock.Setup(c => c.UtcNow).Returns(_now);
        _clock.Setup(c => c.UtcNowOffset).Returns(new DateTimeOffset(_now));
        return new UserNotificationSettingsChangedConsumer(_settingsRepository.Object, _clock.Object, _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenAnnouncementsEnabled_UpsertsWithEmailEnabled()
    {
        UserNotificationSettings? captured = null;
        _settingsRepository
            .Setup(r => r.UpsertAsync(It.IsAny<UserNotificationSettings>(), It.IsAny<CancellationToken>()))
            .Callback<UserNotificationSettings, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);
        var consumer = CreateConsumer();

        var @event = new UserNotificationSettingsChangedV1
        {
            UserId = _userId,
            Email = "user@example.com",
            IsActive = true,
            IsStaff = false,
            EmailAnnouncementsEnabled = true,
            EmailPromotionsEnabled = false
        };

        await consumer.HandleAsync(@event, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(_userId, captured!.UserId);
        Assert.True(captured.EmailEnabled);
        Assert.True(captured.InAppEnabled);
        Assert.Equal(_now, captured.UpdatedAt);
    }

    [Fact]
    public async Task HandleAsync_WhenBothEmailFlagsDisabled_UpsertsWithEmailDisabled()
    {
        UserNotificationSettings? captured = null;
        _settingsRepository
            .Setup(r => r.UpsertAsync(It.IsAny<UserNotificationSettings>(), It.IsAny<CancellationToken>()))
            .Callback<UserNotificationSettings, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);
        var consumer = CreateConsumer();

        var @event = new UserNotificationSettingsChangedV1
        {
            UserId = _userId,
            Email = "user@example.com",
            IsActive = true,
            IsStaff = true,
            EmailAnnouncementsEnabled = false,
            EmailPromotionsEnabled = false
        };

        await consumer.HandleAsync(@event, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.False(captured!.EmailEnabled);
        Assert.True(captured.IsStaff);
    }
}
