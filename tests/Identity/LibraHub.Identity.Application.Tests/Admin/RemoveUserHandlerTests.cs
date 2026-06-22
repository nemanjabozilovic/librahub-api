using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Admin.Commands.RemoveUser;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Admin;

public class RemoveUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public RemoveUserHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
        _configuration.Setup(c => c["Storage:AvatarsBucketName"]).Returns("avatars");
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private RemoveUserHandler CreateHandler() => new(
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _outboxWriter.Object,
        _clock.Object,
        _objectStorage.Object,
        _configuration.Object,
        _unitOfWork.Object,
        Mock.Of<ILogger<RemoveUserHandler>>());

    private static User CreateUser(Role role = Role.User)
    {
        var user = new User(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));
        user.AddRole(role);
        return user;
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new RemoveUserCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_LastAdmin_ReturnsValidationError()
    {
        var user = CreateUser(Role.Admin);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository.Setup(r => r.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new RemoveUserCommand(user.Id, "reason"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_RemovesUser_RevokesTokens_WritesEvent()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new RemoveUserCommand(user.Id, "reason"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Removed, user.Status);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserRemoved, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminButNotLast_RemovesUser()
    {
        var user = CreateUser(Role.Admin);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository.Setup(r => r.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateHandler().Handle(new RemoveUserCommand(user.Id, "reason"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserWithAvatar_DeletesAvatar()
    {
        var user = CreateUser();
        user.UpdateAvatar($"https://gw.test/api/users/{user.Id}/avatar/pic.png");
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new RemoveUserCommand(user.Id, "reason"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _objectStorage.Verify(s => s.DeleteAsync("avatars", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
