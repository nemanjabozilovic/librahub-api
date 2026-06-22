using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUsersByIds;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Queries;

public class GetUsersByIdsQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private GetUsersByIdsQueryHandler CreateHandler() => new(_userRepository.Object);

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_EmptyList_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new GetUsersByIdsQuery(new List<Guid>()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_TooManyIds_ReturnsValidationError()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();

        var result = await CreateHandler().Handle(new GetUsersByIdsQuery(ids), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_SkipsMissingAndRemovedUsers()
    {
        var active = CreateUser();
        var removed = CreateUser();
        removed.Remove("test");
        var missingId = Guid.NewGuid();

        _userRepository.Setup(r => r.GetByIdAsync(active.Id, It.IsAny<CancellationToken>())).ReturnsAsync(active);
        _userRepository.Setup(r => r.GetByIdAsync(removed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(removed);
        _userRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(
            new GetUsersByIdsQuery(new List<Guid> { active.Id, removed.Id, missingId }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Users);
        Assert.Equal(active.Id, result.Value.Users[0].Id);
    }
}
