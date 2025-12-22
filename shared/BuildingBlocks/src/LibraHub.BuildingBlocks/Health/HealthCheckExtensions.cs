using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraHub.BuildingBlocks.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddLibraHubHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "database");

        return services;
    }
}
