using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Commands.Login;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Auth;

public class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRecaptchaService> _recaptchaService = new();
    private readonly Mock<IClock> _clock = new();
    private readonly SecurityOptions _securityOptions = new() { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 };

    public LoginHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);
        _recaptchaService
            .Setup(r => r.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private LoginHandler CreateHandler() => new(
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _passwordHasher.Object,
        _tokenService.Object,
        _recaptchaService.Object,
        _clock.Object,
        Microsoft.Extensions.Options.Options.Create(_securityOptions));

    private static User CreateUser(string email = "user@example.com", string passwordHash = "hash")
        => new(Guid.NewGuid(), email, passwordHash, "Test", "User", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_MissingRecaptchaToken_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(
            new LoginCommand("user@example.com", "pass", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _recaptchaService.Verify(r => r.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidRecaptcha_ReturnsValidationError()
    {
        _recaptchaService
            .Setup(r => r.VerifyAsync("bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().Handle(
            new LoginCommand("user@example.com", "pass", "bad-token"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _userRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsUnauthorized_AndNormalizesEmailToLowercase()
    {
        _userRepository
            .Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(
            new LoginCommand("User@Example.com", "pass", "ok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        _userRepository.Verify(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovedAccount_ReturnsForbidden()
    {
        var user = CreateUser();
        user.Remove("test cleanup");
        _userRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(
            new LoginCommand("user@example.com", "pass", "ok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_LockedOutAccount_ReturnsForbidden()
    {
        var user = CreateUser();
        user.RecordFailedLogin(maxAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15));
        _userRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(
            new LoginCommand("user@example.com", "pass", "ok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        _passwordHasher.Verify(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongPassword_RecordsFailedLogin_ReturnsUnauthorized()
    {
        var user = CreateUser();
        _userRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.VerifyPassword("wrong", "hash")).Returns(false);

        var result = await CreateHandler().Handle(
            new LoginCommand("user@example.com", "wrong", "ok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.Equal(1, user.FailedLoginAttempts);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens_AndPersistsRefreshToken()
    {
        var user = CreateUser();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        _userRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.VerifyPassword("correct", "hash")).Returns(true);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("access-token");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
        _tokenService.Setup(t => t.GetRefreshTokenExpiration()).Returns(expiresAt);

        var result = await CreateHandler().Handle(
            new LoginCommand("user@example.com", "correct", "ok"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        Assert.Equal(expiresAt, result.Value.ExpiresAt);
        Assert.Equal(0, user.FailedLoginAttempts);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
