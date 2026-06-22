using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Announcements.Commands.PublishAnnouncement;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Announcements;
using LibraHub.Contracts.Catalog.V1;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Announcements.Commands.PublishAnnouncement;

public class PublishAnnouncementHandlerTests
{
    private readonly Mock<IAnnouncementRepository> _announcementRepository = new();
    private readonly Mock<IOutboxWriter> _outboxWriter = new();
    private readonly CatalogOptions _options = new() { GatewayBaseUrl = "https://gw", ContentApiUrl = "https://content" };

    private PublishAnnouncementHandler CreateHandler() => new(
        _announcementRepository.Object,
        _outboxWriter.Object,
        Microsoft.Extensions.Options.Options.Create(_options));

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Announcement?)null);

        var result = await CreateHandler().Handle(new PublishAnnouncementCommand(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_DraftWithImage_PublishesWritesEventWithImageUrl()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.DraftWithImage(id, "img.png", Guid.NewGuid());
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(new PublishAnnouncementCommand(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AnnouncementStatus.Published, announcement.Status);
        _announcementRepository.Verify(r => r.UpdateAsync(announcement, It.IsAny<CancellationToken>()), Times.Once);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<AnnouncementPublishedV1>(e => e.AnnouncementId == id && e.ImageUrl == "https://gw/api/announcement-images/img.png"),
            Contracts.Common.EventTypes.AnnouncementPublished,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyPublished_IsNoOpButStillWritesEvent()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.Published(id);
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(new PublishAnnouncementCommand(id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _outboxWriter.Verify(o => o.WriteAsync(
            It.Is<AnnouncementPublishedV1>(e => e.AnnouncementId == id && e.ImageUrl == null),
            Contracts.Common.EventTypes.AnnouncementPublished,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
