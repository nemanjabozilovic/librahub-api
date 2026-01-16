using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LibraHub.Notifications.Api.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            var groupName = $"user-{userId.Value}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(
                "SignalR client connected. ConnectionId: {ConnectionId}, UserId: {UserId}, Group: {GroupName}",
                Context.ConnectionId, userId.Value, groupName);
        }
        else
        {
            _logger.LogWarning(
                "SignalR client connected but userId could not be extracted. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            var groupName = $"user-{userId.Value}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation(
                "SignalR client disconnected. ConnectionId: {ConnectionId}, UserId: {UserId}, Group: {GroupName}, Exception: {Exception}",
                Context.ConnectionId, userId.Value, groupName, exception?.Message);
        }
        else
        {
            _logger.LogWarning(
                "SignalR client disconnected but userId could not be extracted. ConnectionId: {ConnectionId}, Exception: {Exception}",
                Context.ConnectionId, exception?.Message);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid? GetUserId()
    {
        if (Context.User == null)
        {
            return null;
        }

        var nameIdentifierClaim = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var subClaim = Context.User.FindFirst("sub")?.Value;
        var userIdClaim = Context.User.FindFirst("userId")?.Value;

        var claimValue = nameIdentifierClaim ?? subClaim ?? userIdClaim;

        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return null;
        }

        if (Guid.TryParse(claimValue, out var userId))
        {
            return userId;
        }

        return null;
    }
}
