using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Commands.VerifyEmail;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Auth;

public class VerifyEmailHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEmailVerificationTokenRepository> _tokenRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IClock> _clock = new();

    public VerifyEmailHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
    }

    private VerifyEmailHandler CreateHandler() => new(
        _userRepository.Object,
        _tokenRepository.Object,
        _outboxWriter.Object,
        _clock.Object);

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    private static EmailVerificationToken ValidToken(Guid userId)
        => new(Guid.NewGuid(), userId, "tok", DateTime.UtcNow.AddDays(1));

    [Fact]
    public async Task Handle_UnknownToken_ReturnsValidationError()
    {
        _tokenRepository.Setup(r => r.GetByTokenAsync("x", It.IsAny<CancellationToken>())).ReturnsAsync((EmailVerificationToken?)null);

        var result = await CreateHandler().Handle(new VerifyEmailCommand("x"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsValidationError()
    {
        var token = new EmailVerificationToken(Guid.NewGuid(), Guid.NewGuid(), "tok", DateTime.UtcNow.AddMinutes(-1));
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var result = await CreateHandler().Handle(new VerifyEmailCommand("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        var token = ValidToken(Guid.NewGuid());
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new VerifyEmailCommand("tok"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_AlreadyVerified_ReturnsSuccess_WithoutUpdates()
    {
        var user = CreateUser();
        user.MarkEmailAsVerified();
        var token = ValidToken(user.Id);
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new VerifyEmailCommand("tok"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidToken_VerifiesEmail_MarksTokenUsed_WritesEvent()
    {
        var user = CreateUser();
        var token = ValidToken(user.Id);
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new VerifyEmailCommand("tok"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.EmailVerified);
        Assert.True(token.IsUsed);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepository.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.EmailVerified, It.IsAny<CancellationToken>()), Times.Once);
    }
}
