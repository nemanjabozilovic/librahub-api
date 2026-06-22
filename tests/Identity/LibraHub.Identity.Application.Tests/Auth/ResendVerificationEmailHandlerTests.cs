using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Commands.ResendVerificationEmail;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Auth;

public class ResendVerificationEmailHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEmailVerificationTokenRepository> _tokenRepository = new();
    private readonly Mock<IEmailVerificationTokenService> _tokenService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FrontendOptions _frontendOptions = new() { BaseUrl = "https://app.test/" };

    public ResendVerificationEmailHandlerTests()
    {
        _tokenService.Setup(t => t.GenerateToken()).Returns("verify-token");
        _tokenService.Setup(t => t.GetExpiration()).Returns(DateTime.UtcNow.AddDays(1));
        _tokenService.Setup(t => t.GetExpirationDays()).Returns(1);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private ResendVerificationEmailHandler CreateHandler() => new(
        _userRepository.Object,
        _tokenRepository.Object,
        _tokenService.Object,
        _emailSender.Object,
        _unitOfWork.Object,
        Microsoft.Extensions.Options.Options.Create(_frontendOptions),
        Mock.Of<ILogger<ResendVerificationEmailHandler>>());

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", "hash", "Test", "User", null, new DateTime(1990, 1, 1));

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsSuccess_WithoutToken()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(new ResendVerificationEmailCommand("User@Example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyVerified_ReturnsSuccess_WithoutToken()
    {
        var user = CreateUser();
        user.MarkEmailAsVerified();
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new ResendVerificationEmailCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnverifiedUser_CreatesToken_AndSendsEmail()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new ResendVerificationEmailCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(e => e.SendEmailWithTemplateAsync(user.Email, It.IsAny<string>(), "REGISTER", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailSenderThrows_ReturnsFailure()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _emailSender
            .Setup(e => e.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await CreateHandler().Handle(new ResendVerificationEmailCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("INTERNAL_ERROR", result.Error!.Code);
    }
}
