using LibraHub.BuildingBlocks.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace LibraHub.Identity.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IConnection? _rabbitMqConnection;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        IConnection? rabbitMqConnection,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _rabbitMqConnection = rabbitMqConnection;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var health = await _healthCheckService.CheckHealthAsync(_rabbitMqConnection, _logger, cancellationToken);

        return health.Status == "Healthy"
            ? Ok(health)
            : StatusCode(503, health);
    }
}
