using LibraHub.BuildingBlocks.Caching;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Consumers;
using LibraHub.Catalog.Domain.Projections;
using LibraHub.Contracts.Content.V1;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Consumers;

public class CoverUploadedConsumerTests
{
    private readonly Mock<IBookContentStateRepository> _contentStateRepository = new();
    private readonly Mock<ICache> _cache = new();
    private readonly Mock<ILogger<CoverUploadedConsumer>> _logger = new();

    private CoverUploadedConsumer CreateConsumer() => new(
        _contentStateRepository.Object,
        _cache.Object,
        _logger.Object);

    private static CoverUploadedV1 CreateEvent(Guid bookId) => new()
    {
        BookId = bookId,
        CoverRef = "cover.png",
        Sha256 = new string('a', 64),
        Size = 10,
        ContentType = "image/png",
        UploadedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task HandleAsync_NoExistingState_CreatesAndSetsCover()
    {
        var bookId = Guid.NewGuid();
        BookContentState? added = null;
        _contentStateRepository.SetupSequence(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookContentState?)null)
            .ReturnsAsync(() => added);
        _contentStateRepository
            .Setup(r => r.AddAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()))
            .Callback<BookContentState, CancellationToken>((s, _) => added = s)
            .Returns(Task.CompletedTask);

        await CreateConsumer().HandleAsync(CreateEvent(bookId), CancellationToken.None);

        _contentStateRepository.Verify(r => r.AddAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(added);
        Assert.Equal("cover.png", added!.CoverRef);
        _contentStateRepository.Verify(r => r.UpdateAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_ExistingState_UpdatesCoverWithoutAdd()
    {
        var bookId = Guid.NewGuid();
        var existing = new BookContentState(bookId);
        _contentStateRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await CreateConsumer().HandleAsync(CreateEvent(bookId), CancellationToken.None);

        _contentStateRepository.Verify(r => r.AddAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("cover.png", existing.CoverRef);
        Assert.True(existing.HasCover);
        _contentStateRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }
}
