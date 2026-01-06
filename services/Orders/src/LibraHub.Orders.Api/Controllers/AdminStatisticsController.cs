using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Statistics.Queries.GetOrderStatistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Orders.Api.Controllers;

[ApiController]
[Route("admin/statistics")]
[Authorize(Roles = "Admin")]
public class AdminStatisticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("orders")]
    [ProducesResponseType(typeof(OrderStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderStatistics(CancellationToken cancellationToken)
    {
        var query = new GetOrderStatisticsQuery();
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
