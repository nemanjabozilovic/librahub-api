using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Commands.Register;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Auth;

public class RegisterHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEmailVerificationTokenRepository> _tokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IEmailVerificationTokenService> _tokenService = new();
    private readonly Mock<IRecaptchaService> _recaptchaService = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FrontendOptions _frontendOptions = new() { BaseUrl = "https://app.test/" };

    public RegisterHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
        _recaptchaService.Setup(r => r.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _passwordHasher.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _tokenService.Setup(t => t.GenerateToken()).Returns("verify-token");
        _tokenService.Setup(t => t.GetExpiration()).Returns(DateTime.UtcNow.AddDays(1));
        _tokenService.Setup(t => t.GetExpirationDays()).Returns(1);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private RegisterHandler CreateHandler() => new(
        _userRepository.Object,
        _tokenRepository.Object,
        _passwordHasher.Object,
        _tokenService.Object,
        _recaptchaService.Object,
        _outboxWriter.Object,
        _emailSender.Object,
        _clock.Object,
        _unitOfWork.Object,
        Microsoft.Extensions.Options.Options.Create(_frontendOptions),
        Mock.Of<ILogger<RegisterHandler>>());

    private static RegisterCommand Command(string email = "New@Example.com", string? recaptcha = "ok")
        => new(email, "Password1!", "Jane", "Doe", new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero), true, false, null, recaptcha);

    [Fact]
    public async Task Handle_MissingRecaptchaToken_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(Command(recaptcha: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _userRepository.Verify(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidRecaptcha_ReturnsValidationError()
    {
        _recaptchaService.Setup(r => r.VerifyAsync("bad", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(Command(recaptcha: "bad"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsConflict_AndNormalizesEmail()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync("new@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        _userRepository.Verify(r => r.ExistsByEmailAsync("new@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsUserAndToken_AndWritesEvents()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserRegistered, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserNotificationSettingsChanged, It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(e => e.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), "REGISTER", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailSenderThrows_StillSucceeds()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _emailSender
            .Setup(e => e.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
