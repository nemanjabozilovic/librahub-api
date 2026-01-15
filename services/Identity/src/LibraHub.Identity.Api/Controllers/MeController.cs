using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Api.Dtos.Me;
using LibraHub.Identity.Api.Dtos.Users;
using LibraHub.Identity.Application.Me.Commands.UpdateMyProfile;
using LibraHub.Identity.Application.Me.Queries.GetMe;
using LibraHub.Identity.Application.Me.Queries.GetMyProfile;
using LibraHub.Identity.Application.Users.Commands.UpdateNotificationSettings;
using LibraHub.Identity.Application.Users.Commands.UploadAvatar;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MeController(IMediator mediator, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetMeResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var query = new GetMeQuery();
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var query = new GetMyProfileQuery();
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateMyProfileCommand(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Phone,
            request.EmailAnnouncementsEnabled,
            request.EmailPromotionsEnabled);

        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPatch("notification-settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyNotificationSettings(
        [FromBody] UpdateNotificationSettingsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateNotificationSettingsCommand(
            request.EmailAnnouncementsEnabled,
            request.EmailPromotionsEnabled);

        var result = await mediator.Send(command, cancellationToken);
        return result.ToNoContentActionResult(this);
    }

    [HttpPost("avatar")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadMyAvatar([FromForm] IFormFile file, CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Unauthorized(new Error("UNAUTHORIZED", "Not authenticated"));
        }

        var command = new UploadAvatarCommand(currentUser.UserId.Value, file);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
