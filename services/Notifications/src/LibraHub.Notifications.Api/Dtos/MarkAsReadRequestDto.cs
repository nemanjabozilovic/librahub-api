namespace LibraHub.Notifications.Api.Dtos;

public record MarkAsReadRequestDto
{
    public List<Guid> NotificationIds { get; init; } = new();
}
