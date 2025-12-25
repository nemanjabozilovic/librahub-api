using LibraHub.Gateway.Api.Dtos.Dashboard;
using LibraHub.Gateway.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraHub.Gateway.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var authorizationToken = Request.Headers.Authorization.ToString();
        var summary = await _dashboardService.GetSummaryAsync(authorizationToken, cancellationToken);
        return Ok(summary);
    }
}
