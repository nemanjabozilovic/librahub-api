namespace LibraHub.Contracts.Catalog.V1;

public record AnnouncementPublishedV1
{
    public Guid AnnouncementId { get; init; }
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset PublishedAt { get; init; }
}
