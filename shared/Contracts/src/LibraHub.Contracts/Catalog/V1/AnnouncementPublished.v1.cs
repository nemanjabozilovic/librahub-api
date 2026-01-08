namespace LibraHub.Contracts.Catalog.V1;

public record AnnouncementPublishedV1
{
    public Guid AnnouncementId { get; init; }
    public Guid? BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
}
