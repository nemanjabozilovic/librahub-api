using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Preferences.Commands.UpdatePreferences;
using LibraHub.Notifications.Domain.Recipients;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Preferences.Commands.UpdatePreferences;

public class UpdatePreferencesHandlerTests
{
    private readonly Mock<IUserNotificationSettingsRepository> _settingsRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private readonly Guid _userId = Guid.NewGuid();

    private UpdatePreferencesHandler CreateHandler()
    {
        return new UpdatePreferencesHandler(_settingsRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdatePreferencesCommand(true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenSettingsNotFound_ReturnsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _settingsRepository
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotificationSettings?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdatePreferencesCommand(true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesEmailEnabledAndUpserts()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        var settings = new UserNotificationSettings(_userId, "user@example.com", isActive: true, isStaff: false, emailEnabled: false);
        _settingsRepository
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdatePreferencesCommand(true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(settings.EmailEnabled);
        _settingsRepository.Verify(r => r.UpsertAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }
}
