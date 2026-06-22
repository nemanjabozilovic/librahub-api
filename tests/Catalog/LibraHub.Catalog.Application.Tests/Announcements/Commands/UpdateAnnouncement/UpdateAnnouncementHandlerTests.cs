using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Announcements.Commands.UpdateAnnouncement;
using LibraHub.Catalog.Application.Tests.TestHelpers;
using LibraHub.Catalog.Domain.Announcements;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Announcements.Commands.UpdateAnnouncement;

public class UpdateAnnouncementHandlerTests
{
    private readonly Mock<IAnnouncementRepository> _announcementRepository = new();

    private UpdateAnnouncementHandler CreateHandler() => new(_announcementRepository.Object);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Announcement?)null);

        var result = await CreateHandler().Handle(new UpdateAnnouncementCommand(id, null, "T", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_NoChanges_ReturnsValidationError()
    {
        var id = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var announcement = AnnouncementFactory.Draft(id, bookId, "Same", "Same");
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(new UpdateAnnouncementCommand(id, bookId, "Same", "Same"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        _announcementRepository.Verify(r => r.UpdateAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TitleChanged_UpdatesAnnouncement()
    {
        var id = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var announcement = AnnouncementFactory.Draft(id, bookId, "Old", "Content");
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(new UpdateAnnouncementCommand(id, bookId, "New", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", announcement.Title);
        _announcementRepository.Verify(r => r.UpdateAsync(announcement, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BookIdChanged_UpdatesBookId()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.Draft(id, Guid.NewGuid(), "Title", "Content");
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var newBookId = Guid.NewGuid();
        var result = await CreateHandler().Handle(new UpdateAnnouncementCommand(id, newBookId, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newBookId, announcement.BookId);
    }

    [Fact]
    public async Task Handle_PublishedAnnouncement_ReturnsValidationError()
    {
        var id = Guid.NewGuid();
        var announcement = AnnouncementFactory.Published(id, Guid.NewGuid(), "Title", "Content");
        _announcementRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await CreateHandler().Handle(new UpdateAnnouncementCommand(id, announcement.BookId, "New", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }
}
