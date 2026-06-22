using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUsers;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Queries;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private GetUsersQueryHandler CreateHandler() => new(_userRepository.Object);

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_NegativeSkip_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetUsersQuery(-1, 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_TakeOutOfRange_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetUsersQuery(0, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPagedUsers()
    {
        var users = new List<User> { CreateUser(), CreateUser() };
        _userRepository.Setup(r => r.GetUsersPagedAsync(0, 50, It.IsAny<CancellationToken>())).ReturnsAsync(users);
        _userRepository.Setup(r => r.CountAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateHandler().Handle(new GetUsersQuery(0, 50), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Users.Count);
        Assert.Equal(2, result.Value.TotalCount);
    }
}
