using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Me.Queries.GetMyProfile;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Me;

public class GetMyProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly IdentityOptions _identityOptions = new() { GatewayBaseUrl = "https://gw.test" };
    private readonly Guid _userId = Guid.NewGuid();

    public GetMyProfileQueryHandlerTests()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_userId);
    }

    private GetMyProfileQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _currentUser.Object,
        Microsoft.Extensions.Options.Options.Create(_identityOptions),
        Mock.Of<ILogger<GetMyProfileQueryHandler>>());

    private User CreateUser()
        => new(_userId, "user@example.com", "hash", "Test", "User", "+100", new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetMyProfileQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsUnauthorized()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new GetMyProfileQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_RemovedUser_ReturnsForbidden()
    {
        var user = CreateUser();
        user.Remove("test");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetMyProfileQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ActiveUser_ReturnsProfile()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetMyProfileQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Test", result.Value.FirstName);
        Assert.Equal("+100", result.Value.Phone);
    }
}
