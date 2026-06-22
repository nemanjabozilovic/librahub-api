using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Commands.CreateUser;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Commands;

public class CreateUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRegistrationCompletionTokenRepository> _tokenRepository = new();
    private readonly Mock<IRegistrationCompletionTokenService> _tokenService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public CreateUserHandlerTests()
    {
        _tokenService.Setup(t => t.GenerateToken()).Returns("completion-token");
        _tokenService.Setup(t => t.GetExpiration()).Returns(DateTime.UtcNow.AddHours(48));
        _tokenService.Setup(t => t.GetExpirationHours()).Returns(48);
        _configuration.Setup(c => c["Frontend:BaseUrl"]).Returns("https://app.test/");
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private CreateUserHandler CreateHandler() => new(
        _userRepository.Object,
        _tokenRepository.Object,
        _tokenService.Object,
        _emailSender.Object,
        _configuration.Object,
        _unitOfWork.Object,
        Mock.Of<ILogger<CreateUserHandler>>());

    private static CreateUserCommand Command() => new("New@Example.com", Role.Librarian);

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsConflict()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync("new@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsUserAndToken_SendsEmail()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepository.Verify(r => r.AddAsync(It.IsAny<RegistrationCompletionToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSender.Verify(e => e.SendEmailWithTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), "COMPLETE_REGISTRATION", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
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
