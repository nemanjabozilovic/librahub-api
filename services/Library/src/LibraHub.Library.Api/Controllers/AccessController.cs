using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Entitlements.Queries.CheckAccess;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Library.Api.Controllers;

[ApiController]
[Route("api/access")]
public class AccessController(IMediator mediator) : ControllerBase
{
    [HttpGet("check")]
    [Authorize(Policy = "InternalAccess")]
    [ProducesResponseType(typeof(CheckAccessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckAccess(
        [FromQuery] Guid userId,
        [FromQuery] Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var query = new CheckAccessQuery
        {
            UserId = userId,
            BookId = bookId
        };

        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}

