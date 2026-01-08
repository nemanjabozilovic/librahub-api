namespace LibraHub.Catalog.Api.Dtos.Announcements;

public record CreateAnnouncementRequestDto
{
    public Guid? BookId { get; init; } // Optional: null = general announcement (not tied to a specific book)
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
