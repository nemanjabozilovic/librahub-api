using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Announcements.Queries.GetAnnouncements;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Announcements;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Announcements.Queries.GetAnnouncements;

public class GetAnnouncementsHandlerTests
{
    private readonly Mock<IAnnouncementRepository> _announcementRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly CatalogOptions _options = new() { GatewayBaseUrl = "https://gw", ContentApiUrl = "https://content" };

    private GetAnnouncementsHandler CreateHandler() => new(
        _announcementRepository.Object,
        _currentUser.Object,
        Microsoft.Extensions.Options.Options.Create(_options));

    [Fact]
    public async Task Handle_ByBookId_QueriesByBookAndMaps()
    {
        var bookId = Guid.NewGuid();
        var announcement = AnnouncementFactory.DraftWithImage(Guid.NewGuid(), "img.png", bookId);
        _announcementRepository.Setup(r => r.GetByBookIdAsync(bookId, 1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Announcement> { announcement });
        _announcementRepository.Setup(r => r.CountByBookIdAsync(bookId, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new GetAnnouncementsQuery(bookId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("https://gw/api/announcement-images/img.png", result.Value.Announcements[0].ImageUrl);
    }

    [Fact]
    public async Task Handle_NoBookIdAsAnonymous_QueriesPublishedOnly()
    {
        _currentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
        _announcementRepository.Setup(r => r.GetPublishedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Announcement>());
        _announcementRepository.Setup(r => r.CountPublishedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateHandler().Handle(new GetAnnouncementsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _announcementRepository.Verify(r => r.GetPublishedAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
        _announcementRepository.Verify(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoBookIdAsAdmin_QueriesAll()
    {
        _currentUser.Setup(u => u.IsInRole("Admin")).Returns(true);
        _announcementRepository.Setup(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Announcement>());
        _announcementRepository.Setup(r => r.CountAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateHandler().Handle(new GetAnnouncementsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _announcementRepository.Verify(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
        _announcementRepository.Verify(r => r.GetPublishedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
