using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Commands.ForgotPassword;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Auth;

public class ForgotPasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepository = new();
    private readonly Mock<IPasswordResetTokenService> _tokenService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly FrontendOptions _frontendOptions = new() { BaseUrl = "https://app.test/" };

    public ForgotPasswordHandlerTests()
    {
        _tokenService.Setup(t => t.GenerateToken()).Returns("reset-token");
        _tokenService.Setup(t => t.GetExpiration()).Returns(DateTime.UtcNow.AddHours(24));
    }

    private ForgotPasswordHandler CreateHandler() => new(
        _userRepository.Object,
        _tokenRepository.Object,
        _tokenService.Object,
        _emailSender.Object,
        Microsoft.Extensions.Options.Options.Create(_frontendOptions),
        Mock.Of<ILogger<ForgotPasswordHandler>>());

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsSuccess_WithoutCreatingToken()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new ForgotPasswordCommand("User@Example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _userRepository.Verify(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailSender.Verify(e => e.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KnownEmail_CreatesToken_AndSendsEmail()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(e => e.SendEmailWithTemplateAsync(user.Email, It.IsAny<string>(), "FORGOT_PASSWORD", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailSenderThrows_StillSucceeds()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _emailSender
            .Setup(e => e.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await CreateHandler().Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
