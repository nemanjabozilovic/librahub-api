using FluentValidation;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Email;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.Idempotency;
using LibraHub.BuildingBlocks.Messaging;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.Notifications.Api.Workers;
using LibraHub.Notifications.Application;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Infrastructure.Idempotency;
using LibraHub.Notifications.Infrastructure.Options;
using LibraHub.Notifications.Infrastructure.Persistence;
using LibraHub.Notifications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Notifications.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationsDb")
            ?? throw new InvalidOperationException("Connection string 'NotificationsDb' not found.");

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                }));

        return services;
    }

    public static IServiceCollection AddNotificationsApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferencesRepository, NotificationPreferencesRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();
        services.AddScoped<BuildingBlocks.Abstractions.IUnitOfWork, UnitOfWork>();

        // Register SignalR
        services.AddSignalR();

        // Register NotificationHub wrapper
        services.AddScoped<INotificationHub, Hubs.NotificationHubWrapper>();

        // Register notification sender
        services.AddScoped<INotificationSender, Infrastructure.Delivery.NotificationSender>();

        services.AddOptions<NotificationsOptions>().Bind(configuration.GetSection(NotificationsOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddHttpClient<IIdentityClient, Infrastructure.Clients.IdentityClient>();

        services.AddScoped<BuildingBlocks.Abstractions.IOutboxWriter, OutboxEventPublisher<NotificationsDbContext>>();
        services.AddScoped<BuildingBlocks.Abstractions.IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<BuildingBlocks.Abstractions.ICurrentUser, BuildingBlocks.CurrentUser.CurrentUser>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore<NotificationsDbContext, IdempotencyKey>>();

        // Register consumers
        services.AddScoped<Application.Consumers.BookPublishedConsumer>();
        services.AddScoped<Application.Consumers.AnnouncementPublishedConsumer>();
        services.AddScoped<Application.Consumers.OrderPaidConsumer>();
        services.AddScoped<Application.Consumers.OrderRefundedConsumer>();
        services.AddScoped<Application.Consumers.EntitlementGrantedConsumer>();
        services.AddScoped<Application.Consumers.UserRemovedConsumer>();

        // Configure email using BuildingBlocks
        services.AddLibraHubEmail(configuration);

        return services;
    }

    public static IServiceCollection AddNotificationsJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubJwtAuthentication(configuration);
    }

    public static IServiceCollection AddNotificationsRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLibraHubRabbitMq<OutboxPublisherWorkerHelper<NotificationsDbContext>>(configuration);
        services.AddHostedService<NotificationsEventConsumerWorker>();
        return services;
    }

    public static IServiceCollection AddNotificationsHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubHealthChecks(configuration, "NotificationsDb");
    }
}
