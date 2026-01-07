namespace LibraHub.Content.Domain.Books;

public class BookEdition
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public BookFormat Format { get; private set; }
    public int Version { get; private set; }
    public Guid StoredObjectId { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public bool IsBlocked { get; private set; }
    public DateTime? BlockedAt { get; private set; }

    protected BookEdition()
    { } // For EF Core

    public BookEdition(
        Guid id,
        Guid bookId,
        BookFormat format,
        int version,
        Guid storedObjectId)
    {
        Id = id;
        BookId = bookId;
        Format = format;
        Version = version;
        StoredObjectId = storedObjectId;
        UploadedAt = DateTime.UtcNow;
        IsBlocked = false;
    }

    public void Block()
    {
        if (IsBlocked)
        {
            return;
        }

        IsBlocked = true;
        BlockedAt = DateTime.UtcNow;
    }

    public bool IsAccessible => !IsBlocked;
}
