using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Statistics.Queries.GetBookStatistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Catalog.Api.Controllers;

[ApiController]
[Route("admin/statistics")]
[Authorize(Roles = "Librarian,Admin")]
public class AdminStatisticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("books")]
    [ProducesResponseType(typeof(BookStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBookStatistics(CancellationToken cancellationToken)
    {
        var query = new GetBookStatisticsQuery();
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}

