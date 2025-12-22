using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LibraHub.BuildingBlocks.Messaging;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddLibraHubRabbitMq<TOutboxPublisherWorker>(
        this IServiceCollection services,
        IConfiguration configuration) where TOutboxPublisherWorker : class, IHostedService
    {
        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ")
            ?? throw new InvalidOperationException("RabbitMQ connection string not configured");

        var rabbitMqConnection = RabbitMqSetup.CreateConnection(rabbitMqConnectionString);
        services.AddSingleton(rabbitMqConnection);
        services.AddHostedService<TOutboxPublisherWorker>();

        return services;
    }
}
