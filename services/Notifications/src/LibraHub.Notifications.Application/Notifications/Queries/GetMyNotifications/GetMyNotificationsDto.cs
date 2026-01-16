namespace LibraHub.Notifications.Application.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsDto
{
    public List<NotificationDto> Notifications { get; init; } = new();
    public int TotalCount { get; init; }
}

public record NotificationDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
}
