namespace LibraHub.Notifications.Application.Abstractions;

public interface INotificationHub
{
    Task SendToUserAsync(Guid userId, string method, object notification, CancellationToken cancellationToken = default);
}

