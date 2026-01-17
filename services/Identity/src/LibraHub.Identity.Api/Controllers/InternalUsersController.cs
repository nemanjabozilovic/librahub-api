using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Users.Queries.GetInternalUserInfo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("internal/users")]
[Authorize(Policy = "InternalAccess")]
public class InternalUsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}/info")]
    [ProducesResponseType(typeof(InternalUserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserInfo(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetInternalUserInfoQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
