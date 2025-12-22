namespace LibraHub.Catalog.Api.Dtos.Announcements;

public record CreateAnnouncementRequestDto
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
