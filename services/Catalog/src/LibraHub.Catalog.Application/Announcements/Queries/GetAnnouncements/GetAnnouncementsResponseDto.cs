namespace LibraHub.Catalog.Application.Announcements.Queries.GetAnnouncements;

public record GetAnnouncementsResponseDto
{
    public List<AnnouncementDto> Announcements { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record AnnouncementDto
{
    public Guid Id { get; init; }
    public Guid? BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; init; }
}
