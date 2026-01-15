using FluentValidation;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.Messaging;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.BuildingBlocks.Storage;
using LibraHub.Catalog.Api.Workers;
using LibraHub.Catalog.Application;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Infrastructure.Persistence;
using LibraHub.Catalog.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Catalog.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CatalogDb")
            ?? throw new InvalidOperationException("Connection string 'CatalogDb' not found.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                })
                   .UseLazyLoadingProxies());

        return services;
    }

    public static IServiceCollection AddCatalogApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IPricingRepository, PricingRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IBookContentStateRepository, BookContentStateRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();

        services.AddScoped<BuildingBlocks.Abstractions.IOutboxWriter, OutboxEventPublisher<CatalogDbContext>>();
        services.AddScoped<BuildingBlocks.Abstractions.IUnitOfWork, Infrastructure.Persistence.UnitOfWork>();
        services.AddScoped<BuildingBlocks.Abstractions.IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<BuildingBlocks.Abstractions.ICurrentUser, BuildingBlocks.CurrentUser.CurrentUser>();

        services.AddScoped<Application.Consumers.CoverUploadedConsumer>();
        services.AddScoped<Application.Consumers.EditionUploadedConsumer>();

        services.AddOptions<Application.Options.CatalogOptions>()
            .Bind(configuration.GetSection(Application.Options.CatalogOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<Application.Options.UploadOptions>()
            .Bind(configuration.GetSection(Application.Options.UploadOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddLibraHubMinioStorage(configuration);

        services.AddHttpClient<Application.Abstractions.IContentReadClient, Infrastructure.Clients.ContentReadClient>()
            .AddHttpMessageHandler<CorrelationIdHeaderHandler>();
        services.AddTransient<CorrelationIdHeaderHandler>();

        services.AddRedisCache(configuration);

        return services;
    }

    public static IServiceCollection AddCatalogJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubJwtAuthentication(configuration);
    }

    public static IServiceCollection AddCatalogRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLibraHubRabbitMq<OutboxPublisherWorkerHelper<CatalogDbContext>>(configuration);
        services.AddHostedService<CatalogEventConsumerWorker>();
        return services;
    }

    public static IServiceCollection AddCatalogHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubHealthChecks(configuration, "CatalogDb");
    }
}
