namespace LibraHub.Catalog.Api.Dtos.Announcements;

public record UpdateAnnouncementRequestDto
{
    public Guid? BookId { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
}

