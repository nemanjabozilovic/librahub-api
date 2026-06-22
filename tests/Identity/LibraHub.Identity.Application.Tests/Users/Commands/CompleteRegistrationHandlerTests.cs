using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Common;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Commands.CompleteRegistration;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using Moq;
using Xunit;

namespace LibraHub.Identity.Application.Tests.Users.Commands;

public class CompleteRegistrationHandlerTests
{
    private readonly Mock<IRegistrationCompletionTokenRepository> _tokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    public CompleteRegistrationHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNowOffset).Returns(DateTimeOffset.UtcNow);
        _passwordHasher.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private CompleteRegistrationHandler CreateHandler() => new(
        _tokenRepository.Object,
        _userRepository.Object,
        _passwordHasher.Object,
        _outboxWriter.Object,
        _unitOfWork.Object,
        _clock.Object);

    private static User CreateUser()
        => new(Guid.NewGuid(), "user@example.com", string.Empty, string.Empty, string.Empty, null, default);

    private static RegistrationCompletionToken ValidToken(Guid userId)
        => new(Guid.NewGuid(), userId, "tok", DateTime.UtcNow.AddHours(1));

    private static CompleteRegistrationCommand Command()
        => new("tok", "Pass1!", "Pass1!", "Jane", "Doe", new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero), true, false);

    [Fact]
    public async Task Handle_InvalidToken_ReturnsValidationError()
    {
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((RegistrationCompletionToken?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        var token = ValidToken(Guid.NewGuid());
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_CompletesProfile_VerifiesEmail_WritesEvents()
    {
        var user = CreateUser();
        var token = ValidToken(user.Id);
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed", user.PasswordHash);
        Assert.Equal("Jane", user.FirstName);
        Assert.True(user.EmailVerified);
        Assert.True(token.IsUsed);
        _userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _tokenRepository.Verify(r => r.UpdateAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.EmailVerified, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserNotificationSettingsChanged, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyVerifiedUser_DoesNotWriteEmailVerifiedEvent()
    {
        var user = CreateUser();
        user.MarkEmailAsVerified();
        var token = ValidToken(user.Id);
        _tokenRepository.Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.EmailVerified, It.IsAny<CancellationToken>()), Times.Never);
        _outboxWriter.Verify(o => o.WriteAsync(It.IsAny<object>(), EventTypes.UserNotificationSettingsChanged, It.IsAny<CancellationToken>()), Times.Once);
    }
}
