using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetInternalUserInfo;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Queries;

public class GetInternalUserInfoQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private GetInternalUserInfoQueryHandler CreateHandler() => new(
        _userRepository.Object,
        Mock.Of<ILogger<GetInternalUserInfoQueryHandler>>());

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new GetInternalUserInfoQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_RemovedUser_ReturnsNotFound()
    {
        var user = CreateUser();
        user.Remove("test");
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetInternalUserInfoQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ActiveUser_ReturnsInfo()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetInternalUserInfoQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.True(result.Value.IsActive);
    }
}
