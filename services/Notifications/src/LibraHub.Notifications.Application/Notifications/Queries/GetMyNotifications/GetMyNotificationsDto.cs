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
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ReadAt { get; init; }
}

