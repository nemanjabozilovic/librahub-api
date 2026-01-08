using FluentValidation;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.Idempotency;
using LibraHub.BuildingBlocks.Messaging;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.Library.Api.Workers;
using LibraHub.Library.Application;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Infrastructure.Clients;
using LibraHub.Library.Infrastructure.Idempotency;
using LibraHub.Library.Infrastructure.Options;
using LibraHub.Library.Infrastructure.Persistence;
using LibraHub.Library.Infrastructure.Projections;
using LibraHub.Library.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Library.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLibraryDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LibraryDb")
            ?? throw new InvalidOperationException("Connection string 'LibraryDb' not found.");

        services.AddDbContext<LibraryDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                })
                   .UseLazyLoadingProxies());

        return services;
    }

    public static IServiceCollection AddLibraryApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        services.AddScoped<IEntitlementRepository, EntitlementRepository>();
        services.AddScoped<IBookSnapshotStore, BookSnapshotStore>();
        services.AddScoped<IReadingProgressRepository, ReadingProgressRepository>();

        services.AddScoped<BuildingBlocks.Abstractions.IOutboxWriter, OutboxEventPublisher<LibraryDbContext>>();
        services.AddScoped<BuildingBlocks.Abstractions.IUnitOfWork, Infrastructure.Persistence.UnitOfWork>();
        services.AddScoped<BuildingBlocks.Abstractions.IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<BuildingBlocks.Abstractions.ICurrentUser, BuildingBlocks.CurrentUser.CurrentUser>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore<LibraryDbContext, IdempotencyKey>>();
        services.AddScoped<BuildingBlocks.Inbox.IInboxRepository, Infrastructure.Repositories.InboxRepository>();

        services.AddScoped<Application.Consumers.OrderPaidConsumer>();
        services.AddScoped<Application.Consumers.OrderRefundedConsumer>();
        services.AddScoped<Application.Consumers.BookPublishedConsumer>();
        services.AddScoped<Application.Consumers.BookUpdatedConsumer>();
        services.AddScoped<Application.Consumers.BookRemovedConsumer>();
        services.AddScoped<Application.Consumers.UserRemovedConsumer>();

        services.AddOptions<LibraryOptions>().Bind(configuration.GetSection(LibraryOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddMemoryCache();
        services.AddHttpClient<Application.Abstractions.IIdentityClient, IdentityClient>();

        services.AddRedisCache(configuration);

        return services;
    }

    public static IServiceCollection AddLibraryJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLibraHubJwtAuthentication(configuration);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("InternalAccess", policy =>
            {
                policy.RequireAssertion(_ => true);
            });
        });

        return services;
    }

    public static IServiceCollection AddLibraryRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLibraHubRabbitMq<OutboxPublisherWorkerHelper<LibraryDbContext>>(configuration);
        services.AddHostedService<LibraryEventConsumerWorker>();
        return services;
    }

    public static IServiceCollection AddLibraryHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubHealthChecks(configuration, "LibraryDb");
    }
}
