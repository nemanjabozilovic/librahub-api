using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Statistics.Queries.GetUserStatistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("admin/statistics")]
[Authorize(Roles = "Admin")]
public class AdminStatisticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserStatistics(CancellationToken cancellationToken)
    {
        var query = new GetUserStatisticsQuery();
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
