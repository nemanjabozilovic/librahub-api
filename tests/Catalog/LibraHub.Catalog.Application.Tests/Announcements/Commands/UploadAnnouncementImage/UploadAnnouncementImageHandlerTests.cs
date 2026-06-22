using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Announcements.Commands.UploadAnnouncementImage;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Announcements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Announcements.Commands.UploadAnnouncementImage;

public class UploadAnnouncementImageHandlerTests
{
    private readonly Mock<IAnnouncementRepository> _announcementRepository = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<ILogger<UploadAnnouncementImageHandler>> _logger = new();
    private readonly UploadOptions _uploadOptions = new() { AnnouncementImagesBucketName = "announcement-images" };
    private readonly CatalogOptions _catalogOptions = new() { GatewayBaseUrl = "https://gw", ContentApiUrl = "https://content" };

    private UploadAnnouncementImageHandler CreateHandler() => new(
        _announcementRepository.Object,
        _objectStorage.Object,
        Microsoft.Extensions.Options.Options.Create(_uploadOptions),
        Microsoft.Extensions.Options.Options.Create(_catalogOptions),
        _logger.Object);

    private static UploadAnnouncementImageCommand CreateCommand(Guid id)
        => new(id, new InMemoryFormFile(new byte[] { 1, 2, 3 }, "image/png", "img.png"));

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Announcement?)null);

        var result = await CreateHandler().Handle(CreateCommand(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_PublishedAnnouncement_ReturnsValidationError()
    {
        var id = Guid.NewGuid();
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(AnnouncementFactory.Published(id));

        var result = await CreateHandler().Handle(CreateCommand(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _objectStorage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DraftWithoutImage_UploadsSetsImageAndReturnsUrl()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.Draft(id);
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(CreateCommand(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("https://gw/api/announcement-images/announcements/", result.Value);
        Assert.NotNull(announcement.ImageRef);
        _objectStorage.Verify(s => s.UploadAsync("announcement-images", It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
        _announcementRepository.Verify(r => r.UpdateAsync(announcement, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DraftWithExistingImage_DeletesOldThenUploads()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.DraftWithImage(id, "old.png");
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(CreateCommand(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _objectStorage.Verify(s => s.DeleteAsync("announcement-images", "old.png", It.IsAny<CancellationToken>()), Times.Once);
        _objectStorage.Verify(s => s.UploadAsync("announcement-images", It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UploadThrows_ReturnsValidationError()
    {
        var id = Guid.NewGuid();
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(AnnouncementFactory.Draft(id));
        _objectStorage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await CreateHandler().Handle(CreateCommand(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _announcementRepository.Verify(r => r.UpdateAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
