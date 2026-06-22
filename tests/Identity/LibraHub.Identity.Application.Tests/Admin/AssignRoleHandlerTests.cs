using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Admin.Commands.AssignRole;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Admin;

public class AssignRoleHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IClock> _clock = new();

    public AssignRoleHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
    }

    private AssignRoleHandler CreateHandler() => new(
        _userRepository.Object,
        _outboxWriter.Object,
        _clock.Object);

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

        var result = await CreateHandler().Handle(new AssignRoleCommand(Guid.NewGuid(), Role.Librarian, true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_RemoveLastAdmin_ReturnsValidationError()
    {
        var user = CreateUser(Role.Admin);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepository.Setup(r => r.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new AssignRoleCommand(user.Id, Role.Admin, false), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AssignRole_AddsRole_WritesEvents()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new AssignRoleCommand(user.Id, Role.Librarian, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.HasRole(Role.Librarian));
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.RoleAssigned, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserNotificationSettingsChanged, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemoveRole_RemovesRole()
    {
        var user = CreateUser(Role.Librarian);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new AssignRoleCommand(user.Id, Role.Librarian, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.HasRole(Role.Librarian));
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
