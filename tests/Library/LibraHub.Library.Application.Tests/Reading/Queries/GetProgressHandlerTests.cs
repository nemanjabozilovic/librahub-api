using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Application.Reading.Queries.GetProgress;
using LibraHub.Library.Domain.Reading;
using Moq;
using Xunit;

namespace LibraHub.Library.Application.Tests.Reading.Queries;

public class GetProgressHandlerTests
{
    private readonly Mock<IReadingProgressRepository> _progressRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetProgressHandler CreateHandler() =>
        new(_progressRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.SetupGet(c => c.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(
            new GetProgressQuery { BookId = Guid.NewGuid() }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NoProgress_ReturnsZeroDefault()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);
        _progressRepository
            .Setup(r => r.GetByUserBookFormatAndVersionAsync(userId, bookId, "PDF", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadingProgress?)null);

        var result = await CreateHandler().Handle(
            new GetProgressQuery { BookId = bookId, Format = "pdf", Version = 2 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(bookId, result.Value.BookId);
        Assert.Equal("PDF", result.Value.Format);
        Assert.Equal(0, result.Value.Percentage);
        Assert.Null(result.Value.LastPage);
    }

    [Fact]
    public async Task Handle_ExistingProgress_ReturnsStoredValues()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        _currentUser.SetupGet(c => c.UserId).Returns(userId);

        var progress = new ReadingProgress(Guid.NewGuid(), userId, bookId, "PDF", 2);
        progress.UpdateProgress(55m, 120);
        _progressRepository
            .Setup(r => r.GetByUserBookFormatAndVersionAsync(userId, bookId, "PDF", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var result = await CreateHandler().Handle(
            new GetProgressQuery { BookId = bookId, Format = "pdf", Version = 2 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(55m, result.Value.Percentage);
        Assert.Equal(120, result.Value.LastPage);
    }
}
