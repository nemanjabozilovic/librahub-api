using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Announcements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementHandlerTests
{
    private readonly Mock<IAnnouncementRepository> _announcementRepository = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<DeleteAnnouncementHandler>> _logger = new();
    private readonly UploadOptions _uploadOptions = new() { AnnouncementImagesBucketName = "announcement-images" };

    public DeleteAnnouncementHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((a, ct) => a(ct));
    }

    private DeleteAnnouncementHandler CreateHandler() => new(
        _announcementRepository.Object,
        _objectStorage.Object,
        Microsoft.Extensions.Options.Options.Create(_uploadOptions),
        _unitOfWork.Object,
        _logger.Object);

    [Fact]
    public async Task Handle_EmptyIds_ReturnsValidationError()
    {
        var result = await CreateHandler().Handle(new DeleteAnnouncementCommand(new List<Guid>()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_OneIdNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Announcement?)null);

        var result = await CreateHandler().Handle(new DeleteAnnouncementCommand(new List<Guid> { id }), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        _announcementRepository.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Announcement>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithImages_DeletesStorageAndRange()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.DraftWithImage(id, "img.png");
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(new DeleteAnnouncementCommand(new List<Guid> { id }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _objectStorage.Verify(s => s.DeleteAsync("announcement-images", "img.png", It.IsAny<CancellationToken>()), Times.Once);
        _announcementRepository.Verify(r => r.DeleteRangeAsync(It.Is<IEnumerable<Announcement>>(a => a.Contains(announcement)), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StorageDeleteFails_StillDeletesRange()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.DraftWithImage(id, "img.png");
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);
        _objectStorage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var result = await CreateHandler().Handle(new DeleteAnnouncementCommand(new List<Guid> { id }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _announcementRepository.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Announcement>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
