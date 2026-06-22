using LibraHub.BuildingBlocks.Caching;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Consumers;
using LibraHub.Catalog.Domain.Projections;
using LibraHub.Contracts.Content.V1;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Consumers;

public class EditionUploadedConsumerTests
{
    private readonly Mock<IBookContentStateRepository> _contentStateRepository = new();
    private readonly Mock<ICache> _cache = new();
    private readonly Mock<ILogger<EditionUploadedConsumer>> _logger = new();

    private EditionUploadedConsumer CreateConsumer() => new(
        _contentStateRepository.Object,
        _cache.Object,
        _logger.Object);

    private static EditionUploadedV1 CreateEvent(Guid bookId) => new()
    {
        BookId = bookId,
        Format = "pdf",
        Version = 1,
        EditionRef = "edition.pdf",
        Sha256 = new string('a', 64),
        Size = 10,
        ContentType = "application/pdf",
        UploadedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task HandleAsync_NoExistingState_CreatesAndSetsEdition()
    {
        var bookId = Guid.NewGuid();
        BookContentState? added = null;
        _contentStateRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync((BookContentState?)null);
        _contentStateRepository
            .Setup(r => r.AddAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()))
            .Callback<BookContentState, CancellationToken>((s, _) => added = s)
            .Returns(Task.CompletedTask);

        await CreateConsumer().HandleAsync(CreateEvent(bookId), CancellationToken.None);

        _contentStateRepository.Verify(r => r.AddAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(added);
        Assert.True(added!.HasEdition);
        _contentStateRepository.Verify(r => r.UpdateAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_ExistingState_SetsEditionWithoutAdd()
    {
        var bookId = Guid.NewGuid();
        var existing = new BookContentState(bookId);
        _contentStateRepository.Setup(r => r.GetByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await CreateConsumer().HandleAsync(CreateEvent(bookId), CancellationToken.None);

        _contentStateRepository.Verify(r => r.AddAsync(It.IsAny<BookContentState>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(existing.HasEdition);
        _contentStateRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }
}
