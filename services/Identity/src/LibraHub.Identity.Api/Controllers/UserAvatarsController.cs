using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Users.Queries.GetUserAvatar;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("users/{userId:guid}/avatar")]
public class UserAvatarsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar(
        [FromRoute] Guid userId,
        [FromRoute] string fileName,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserAvatarQuery(userId, fileName);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return File(result.Value.Content, result.Value.ContentType);
    }
}
