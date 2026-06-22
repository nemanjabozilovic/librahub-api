using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Reading.Commands.UpdateProgress;
using LibraHub.Library.Domain.Reading;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Reading.Commands;

public class UpdateProgressHandlerTests
{
    private readonly Mock<IReadingProgressRepository> _progressRepository = new();
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private UpdateProgressHandler CreateHandler() =>
        new(_progressRepository.Object, _entitlementRepository.Object, _currentUser.Object);

    private static UpdateProgressCommand Command(Guid bookId) => new()
    {
        BookId = bookId,
        Format = "epub",
        Version = 1,
        Percentage = 42m,
        LastPage = 10
    };

    [Fact]
    public async Task Handle_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(Command(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NoAccess_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _entitlementRepository
            .Setup(r => r.HasAccessAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().Handle(Command(bookId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _progressRepository.Verify(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Never);
        _progressRepository.Verify(r => r.UpdateAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoExistingProgress_CreatesProgress()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _entitlementRepository
            .Setup(r => r.HasAccessAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _progressRepository
            .Setup(r => r.GetByUserBookFormatAndVersionAsync(userId, bookId, "EPUB", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadingProgress?)null);

        ReadingProgress? added = null;
        _progressRepository
            .Setup(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()))
            .Callback<ReadingProgress, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(Command(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal("EPUB", added!.Format);
        Assert.Equal(42m, added.ProgressPercentage);
        _progressRepository.Verify(r => r.UpdateAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingProgress_UpdatesProgress()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(false);
        _entitlementRepository
            .Setup(r => r.HasAccessAsync(userId, bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var existing = new ReadingProgress(Guid.NewGuid(), userId, bookId, "EPUB", 1);
        _progressRepository
            .Setup(r => r.GetByUserBookFormatAndVersionAsync(userId, bookId, "EPUB", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(Command(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(42m, existing.ProgressPercentage);
        _progressRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _progressRepository.Verify(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdminRole_SkipsAccessCheck()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _progressRepository
            .Setup(r => r.GetByUserBookFormatAndVersionAsync(userId, bookId, "EPUB", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadingProgress?)null);
        _progressRepository
            .Setup(r => r.AddAsync(It.IsAny<ReadingProgress>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(Command(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _entitlementRepository.Verify(r => r.HasAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
