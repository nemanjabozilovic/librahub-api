using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Commands.UpdateNotificationSettings;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Commands;

public class UpdateNotificationSettingsHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public UpdateNotificationSettingsHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
    }

    private UpdateNotificationSettingsHandler CreateHandler() => new(
        _userRepository.Object,
        _currentUser.Object,
        _outboxWriter.Object,
        _clock.Object);

    private User CreateUser()
        => new(_userId, "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new UpdateNotificationSettingsCommand(true, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NoSettingsProvided_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new UpdateNotificationSettingsCommand(null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new UpdateNotificationSettingsCommand(true, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesPreferences_WritesEvent()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new UpdateNotificationSettingsCommand(true, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.EmailAnnouncementsEnabled);
        Assert.False(user.EmailPromotionsEnabled);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserNotificationSettingsChanged, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpdate_KeepsExistingPromotions()
    {
        var user = CreateUser();
        user.SetEmailNotificationPreferences(false, true);
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new UpdateNotificationSettingsCommand(true, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.EmailAnnouncementsEnabled);
        Assert.True(user.EmailPromotionsEnabled);
    }
}
