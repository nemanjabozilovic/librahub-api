namespace LibraHub.Notifications.Api.Dtos;

public record DeleteNotificationsRequestDto
{
    public List<Guid> NotificationIds { get; init; } = new();
}
