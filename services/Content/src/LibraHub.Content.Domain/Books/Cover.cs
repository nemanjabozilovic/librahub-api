namespace LibraHub.Content.Domain.Books;

public class Cover
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid StoredObjectId { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public bool IsBlocked { get; private set; }
    public DateTime? BlockedAt { get; private set; }

    protected Cover()
    { } // For EF Core

    public Cover(
        Guid id,
        Guid bookId,
        Guid storedObjectId)
    {
        Id = id;
        BookId = bookId;
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
