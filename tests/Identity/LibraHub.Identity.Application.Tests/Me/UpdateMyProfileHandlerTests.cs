using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Me.Commands.UpdateMyProfile;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Me;

public class UpdateMyProfileHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IdentityOptions _identityOptions = new() { GatewayBaseUrl = "https://gw.test" };
    private readonly Guid _userId = Guid.NewGuid();

    public UpdateMyProfileHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
    }

    private UpdateMyProfileHandler CreateHandler() => new(
        _userRepository.Object,
        _currentUser.Object,
        _outboxWriter.Object,
        _clock.Object,
        Microsoft.Extensions.Options.Options.Create(_identityOptions));

    private User CreateUser()
        => new(_userId, "user@example.com", "hash", "Old", "Name", null, new DateTime(1990, 1, 1));

    private static UpdateMyProfileCommand Command()
        => new("Jane", "Doe", new DateTimeOffset(1991, 2, 2, 0, 0, 0, TimeSpan.Zero), "+100", true, false);

    [Fact]
    public async Task Handle_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_RemovedUser_ReturnsForbidden()
    {
        var user = CreateUser();
        user.Remove("test");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesProfile_WritesEvent()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
        Assert.Equal("+100", result.Value.Phone);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserNotificationSettingsChanged, It.IsAny<CancellationToken>()), Times.Once);
    }
}
