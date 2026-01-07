namespace LibraHub.Catalog.Domain.Announcements;

public class Announcement
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public AnnouncementStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Announcement()
    { } // For EF Core

    public Announcement(Guid id, Guid bookId, string title, string content)
    {
        Id = id;
        BookId = bookId;
        Title = title;
        Content = content;
        Status = AnnouncementStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string? title = null, string? content = null)
    {
        if (Status == AnnouncementStatus.Published)
        {
            throw new InvalidOperationException("Cannot update published announcement");
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            Content = content;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status == AnnouncementStatus.Published)
        {
            return; // Already published
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("Title is required for publishing");
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            throw new InvalidOperationException("Content is required for publishing");
        }

        Status = AnnouncementStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
