namespace LibraHub.Library.Domain.Reading;

public class ReadingProgress
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }
    public string? Format { get; private set; }
    public int? Version { get; private set; }
    public decimal ProgressPercentage { get; private set; }
    public int? LastPage { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    protected ReadingProgress()
    {
    } // For EF Core

    public ReadingProgress(
        Guid id,
        Guid userId,
        Guid bookId,
        string? format = null,
        int? version = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (bookId == Guid.Empty)
            throw new ArgumentException("BookId cannot be empty", nameof(bookId));

        Id = id;
        UserId = userId;
        BookId = bookId;
        Format = format;
        Version = version;
        ProgressPercentage = 0;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(decimal percentage, int? lastPage = null)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentException("Progress percentage must be between 0 and 100", nameof(percentage));

        ProgressPercentage = percentage;
        LastPage = lastPage;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
