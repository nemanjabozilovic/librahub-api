using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Preferences.Commands.UpdatePreferences;
using LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;
using LibraHub.Notifications.Domain.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Notifications.Api.Controllers;

[ApiController]
[Route("api/notifications/preferences")]
[Authorize]
public class PreferencesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken = default)
    {
        var query = new GetPreferencesQuery();
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationType>(request.Type, out var notificationType))
        {
            return BadRequest(new Error("INVALID_TYPE", "Invalid notification type"));
        }

        var command = new UpdatePreferencesCommand(
            notificationType,
            request.EmailEnabled,
            request.InAppEnabled);

        await mediator.Send(command, cancellationToken);
        return Ok();
    }
}

public record UpdatePreferencesRequestDto
{
    public string Type { get; init; } = string.Empty;
    public bool EmailEnabled { get; init; }
    public bool InAppEnabled { get; init; }
}
