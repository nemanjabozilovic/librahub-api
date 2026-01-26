using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Api.Dtos;
using LibraHub.Notifications.Application.Notifications.Commands.DeleteNotifications;
using LibraHub.Notifications.Application.Notifications.Commands.MarkAsRead;
using LibraHub.Notifications.Application.Notifications.Queries.GetMyNotifications;
using LibraHub.Notifications.Application.Notifications.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Notifications.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetMyNotificationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyNotificationsQuery(skip, take);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
        [FromBody] MarkAsReadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkAsReadCommand(request.NotificationIds);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var query = new GetUnreadCountQuery();
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteNotifications(
        [FromBody] DeleteNotificationsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteNotificationsCommand(request.NotificationIds);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }
}
