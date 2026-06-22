using LibraHub.Catalog.Domain.Announcements;

namespace LibraHub.Catalog.Application.Tests.TestHelpers;

public static class AnnouncementFactory
{
    public static Announcement Draft(Guid id, Guid? bookId = null, string title = "Title", string content = "Content")
        => new(id, bookId, title, content);

    public static Announcement Published(Guid id, Guid? bookId = null, string title = "Title", string content = "Content")
    {
        var announcement = Draft(id, bookId, title, content);
        announcement.Publish();
        return announcement;
    }

    public static Announcement DraftWithImage(Guid id, string imageRef, Guid? bookId = null)
    {
        var announcement = Draft(id, bookId);
        announcement.SetImage(imageRef);
        return announcement;
    }
}
