using FluentValidation;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.Messaging;
using LibraHub.Catalog.Application;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Infrastructure.Messaging;
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
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddCatalogApplicationServices(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        // Repositories
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IPricingRepository, PricingRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IBookContentStateRepository, BookContentStateRepository>();

        // Infrastructure services
        services.AddScoped<BuildingBlocks.Abstractions.IOutboxWriter, CatalogEventPublisher>();
        services.AddScoped<BuildingBlocks.Abstractions.IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<BuildingBlocks.Abstractions.ICurrentUser, Infrastructure.CurrentUser.CurrentUser>();

        return services;
    }

    public static IServiceCollection AddCatalogJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubJwtAuthentication(configuration);
    }

    public static IServiceCollection AddCatalogRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubRabbitMq<CatalogOutboxPublisherWorker>(configuration);
    }

    public static IServiceCollection AddCatalogHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubHealthChecks(configuration, "CatalogDb");
    }
}
