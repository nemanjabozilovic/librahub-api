using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetRemovedUsers;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Queries;

public class GetRemovedUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private GetRemovedUsersQueryHandler CreateHandler() => new(_userRepository.Object);

    private static User CreateRemovedUser()
    {
        var user = new User(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));
        user.Remove("test");
        return user;
    }

    [Fact]
    public async Task Handle_NegativeSkip_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetRemovedUsersQuery(-1, 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_TakeOutOfRange_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetRemovedUsersQuery(0, 200), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPagedRemovedUsers()
    {
        var users = new List<User> { CreateRemovedUser() };
        _userRepository.Setup(r => r.GetRemovedUsersPagedAsync(0, 50, It.IsAny<CancellationToken>())).ReturnsAsync(users);
        _userRepository.Setup(r => r.CountRemovedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new GetRemovedUsersQuery(0, 50), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Users);
        Assert.Equal(1, result.Value.TotalCount);
    }
}
