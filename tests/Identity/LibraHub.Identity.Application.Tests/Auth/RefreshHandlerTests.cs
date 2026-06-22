using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Commands.Refresh;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Auth;

public class RefreshHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private RefreshHandler CreateHandler() => new(
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _tokenService.Object);

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    private static RefreshToken ActiveToken(Guid userId)
        => new(Guid.NewGuid(), userId, "refresh", DateTime.UtcNow.AddDays(1));

    [Fact]
    public async Task Handle_UnknownToken_ReturnsUnauthorized()
    {
        _refreshTokenRepository
            .Setup(r => r.GetByTokenAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await CreateHandler().Handle(new RefreshCommand("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsUnauthorized()
    {
        var token = ActiveToken(Guid.NewGuid());
        token.Revoke();
        _refreshTokenRepository
            .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await CreateHandler().Handle(new RefreshCommand("refresh"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RemovedUser_ReturnsForbidden()
    {
        var user = CreateUser();
        user.Remove("test");
        var token = ActiveToken(user.Id);
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new RefreshCommand("refresh"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_MissingUser_ReturnsForbidden()
    {
        var token = ActiveToken(Guid.NewGuid());
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new RefreshCommand("refresh"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesToken_AndReturnsNewTokens()
    {
        var user = CreateUser();
        var token = ActiveToken(user.Id);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        _refreshTokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("access");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");
        _tokenService.Setup(t => t.GetRefreshTokenExpiration()).Returns(expiresAt);

        var result = await CreateHandler().Handle(new RefreshCommand("refresh"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal("new-refresh", result.Value.RefreshToken);
        Assert.Equal(expiresAt, result.Value.ExpiresAt);
        Assert.True(token.IsRevoked);
        _refreshTokenRepository.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
