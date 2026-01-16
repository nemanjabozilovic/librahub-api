using LibraHub.Notifications.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace LibraHub.Notifications.Api.Hubs;

public class NotificationHubWrapper : INotificationHub
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ILogger<NotificationHubWrapper> _logger;

    public NotificationHubWrapper(IHubContext<NotificationsHub> hubContext, ILogger<NotificationHubWrapper> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task SendToUserAsync(Guid userId, string method, object notification, CancellationToken cancellationToken = default)
    {
        var groupName = $"user-{userId}";
        _logger.LogInformation(
            "Sending SignalR message to group '{GroupName}' via method '{Method}' for UserId: {UserId}",
            groupName, method, userId);

        return _hubContext.Clients.Group(groupName).SendAsync(method, notification, cancellationToken);
    }
}
