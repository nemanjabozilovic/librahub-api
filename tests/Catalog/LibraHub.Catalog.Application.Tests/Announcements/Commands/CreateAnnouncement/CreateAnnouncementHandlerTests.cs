using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Announcements.Commands.CreateAnnouncement;
using LibraHub.Catalog.Domain.Announcements;
using Moq;
using Xunit;

namespace LibraHub.Catalog.Application.Tests.Announcements.Commands.CreateAnnouncement;

public class CreateAnnouncementHandlerTests
{
    private readonly Mock<IAnnouncementRepository> _announcementRepository = new();

    private CreateAnnouncementHandler CreateHandler() => new(_announcementRepository.Object);

    [Fact]
    public async Task Handle_ValidCommand_PersistsDraftAndReturnsId()
    {
        Announcement? captured = null;
        _announcementRepository
            .Setup(r => r.AddAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()))
            .Callback<Announcement, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var bookId = Guid.NewGuid();
        var result = await CreateHandler().Handle(new CreateAnnouncementCommand(bookId, "Title", "Content"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(result.Value, captured!.Id);
        Assert.Equal("Title", captured.Title);
        Assert.Equal(bookId, captured.BookId);
        Assert.Equal(AnnouncementStatus.Draft, captured.Status);
    }
}
