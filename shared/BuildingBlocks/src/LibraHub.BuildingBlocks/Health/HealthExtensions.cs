using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LibraHub.BuildingBlocks.Health;

public static class HealthExtensions
{
    public static async Task<HealthResponseDto> CheckHealthAsync(
        this HealthCheckService healthCheckService,
        IConnection? rabbitMqConnection,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var checks = new List<HealthCheckResultDto>();

        // Check PostgreSQL
        var dbHealth = await healthCheckService.CheckHealthAsync(
            predicate: check => check.Tags.Contains("database"),
            cancellationToken: cancellationToken);

        string? dbDescription = null;
        if (dbHealth.Status != HealthStatus.Healthy && dbHealth.Entries.Any())
        {
            var dbEntry = dbHealth.Entries.Values.First();
            dbDescription = dbEntry.Description;
        }

        checks.Add(new HealthCheckResultDto
        {
            Name = "postgres",
            Status = dbHealth.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
            Description = dbDescription
        });

        // Check RabbitMQ
        var rabbitMqHealthy = CheckRabbitMq(rabbitMqConnection, logger);
        checks.Add(new HealthCheckResultDto
        {
            Name = "rabbitmq",
            Status = rabbitMqHealthy ? "Healthy" : "Unhealthy",
            Description = rabbitMqHealthy ? null : "Connection failed"
        });

        var allHealthy = checks.All(c => c.Status == "Healthy");

        return new HealthResponseDto
        {
            Status = allHealthy ? "Healthy" : "Unhealthy",
            Checks = checks
        };
    }

    private static bool CheckRabbitMq(IConnection? connection, ILogger logger)
    {
        try
        {
            if (connection == null)
            {
                return false;
            }

            if (!connection.IsOpen)
            {
                return false;
            }

            // Try to create a channel to verify connection is working
            using var channel = connection.CreateModel();
            return channel.IsOpen;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RabbitMQ health check failed");
            return false;
        }
    }
}
