using LibraHub.BuildingBlocks.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace LibraHub.Notifications.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController(
    HealthCheckService healthCheckService,
    IConnection? rabbitMqConnection,
    ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var health = await healthCheckService.CheckHealthAsync(rabbitMqConnection, logger, cancellationToken);

        return health.Status == "Healthy"
            ? Ok(health)
            : StatusCode(503, health);
    }
}
