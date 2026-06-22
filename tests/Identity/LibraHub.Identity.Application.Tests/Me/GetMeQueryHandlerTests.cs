using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Me.Queries.GetMe;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Me;

public class GetMeQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly IdentityOptions _identityOptions = new() { GatewayBaseUrl = "https://gw.test" };
    private readonly Guid _userId = Guid.NewGuid();

    public GetMeQueryHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
    }

    private GetMeQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _currentUser.Object,
        Microsoft.Extensions.Options.Options.Create(_identityOptions),
        Mock.Of<ILogger<GetMeQueryHandler>>());

    private User CreateUser()
    {
        var user = new User(_userId, "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));
        user.AddRole(Role.User);
        return user;
    }

    [Fact]
    public async Task Handle_NoUserId_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetMeQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsUnauthorized()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new GetMeQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_RemovedUser_ReturnsForbidden()
    {
        var user = CreateUser();
        user.Remove("test");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetMeQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ActiveUser_ReturnsProfile()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetMeQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_userId, result.Value.UserId);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.Contains("User", result.Value.Roles);
    }
}
