using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Commands.UpdateUser;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Commands;

public class UpdateUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private UpdateUserHandler CreateHandler() => new(_userRepository.Object);

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Old", "Name", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(
            new UpdateUserCommand(Guid.NewGuid(), "Jane", "Doe", new DateTime(1991, 2, 2)), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesProfile()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(
            new UpdateUserCommand(user.Id, "Jane", "Doe", new DateTime(1991, 2, 2), "+100"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("+100", user.Phone);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailVerifiedTrue_MarksEmailVerified()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(
            new UpdateUserCommand(user.Id, "Jane", "Doe", new DateTime(1991, 2, 2), null, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.EmailVerified);
    }
}
