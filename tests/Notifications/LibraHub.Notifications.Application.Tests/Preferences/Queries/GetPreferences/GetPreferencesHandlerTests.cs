using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;
using LibraHub.Notifications.Domain.Recipients;
using Moq;
using Xunit;

namespace LibraHub.Notifications.Application.Tests.Preferences.Queries.GetPreferences;

public class GetPreferencesHandlerTests
{
    private readonly Mock<IUserNotificationSettingsRepository> _settingsRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private readonly Guid _userId = Guid.NewGuid();

    private GetPreferencesHandler CreateHandler()
    {
        return new GetPreferencesHandler(_settingsRepository.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPreferencesQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenSettingsNull_ReturnsEmailDisabled()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _settingsRepository
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotificationSettings?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPreferencesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.EmailEnabled);
    }

    [Fact]
    public async Task Handle_WhenSettingsExist_ReturnsEmailEnabledFromSettings()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        var settings = new UserNotificationSettings(_userId, "user@example.com", isActive: true, isStaff: false, emailEnabled: true);
        _settingsRepository
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPreferencesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.EmailEnabled);
    }
}
