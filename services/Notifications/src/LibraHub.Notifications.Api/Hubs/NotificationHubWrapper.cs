using LibraHub.Notifications.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace LibraHub.Notifications.Api.Hubs;

public class NotificationHubWrapper : INotificationHub
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public NotificationHubWrapper(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendToUserAsync(Guid userId, string method, object notification, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Group($"user-{userId}").SendAsync(method, notification, cancellationToken);
    }
}

