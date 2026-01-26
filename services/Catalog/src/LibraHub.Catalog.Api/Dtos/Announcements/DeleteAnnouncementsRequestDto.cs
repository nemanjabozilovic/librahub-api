namespace LibraHub.Catalog.Api.Dtos.Announcements;

public record DeleteAnnouncementsRequestDto
{
    public List<Guid> AnnouncementIds { get; init; } = new();
}
