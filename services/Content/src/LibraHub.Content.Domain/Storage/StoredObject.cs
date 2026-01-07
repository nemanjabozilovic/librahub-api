namespace LibraHub.Content.Domain.Storage;

public class StoredObject
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public Sha256 Checksum { get; private set; } = null!;
    public ObjectStatus Status { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? BlockedAt { get; private set; }
    public string? BlockReason { get; private set; }

    protected StoredObject()
    { } // For EF Core

    public StoredObject(
        Guid id,
        Guid bookId,
        string objectKey,
        string contentType,
        long sizeBytes,
        Sha256 checksum)
    {
        Id = id;
        BookId = bookId;
        ObjectKey = objectKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Checksum = checksum;
        Status = ObjectStatus.Active;
        UploadedAt = DateTime.UtcNow;
    }

    public void Block(string reason)
    {
        if (Status == ObjectStatus.Blocked)
        {
            return; // Already blocked
        }

        Status = ObjectStatus.Blocked;
        BlockedAt = DateTime.UtcNow;
        BlockReason = reason;
    }

    public bool IsAccessible => Status == ObjectStatus.Active;
}
