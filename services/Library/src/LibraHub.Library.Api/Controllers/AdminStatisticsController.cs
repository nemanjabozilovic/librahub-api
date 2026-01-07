using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Statistics.Queries.GetEntitlementStatistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Library.Api.Controllers;

[ApiController]
[Route("api/admin/statistics")]
[Authorize(Roles = "Admin")]
public class AdminStatisticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("entitlements")]
    [ProducesResponseType(typeof(EntitlementStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEntitlementStatistics(CancellationToken cancellationToken)
    {
        var query = new GetEntitlementStatisticsQuery();
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
