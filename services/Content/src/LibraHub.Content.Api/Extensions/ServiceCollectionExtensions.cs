using FluentValidation;
using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.Messaging;
using LibraHub.BuildingBlocks.Options;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.BuildingBlocks.Storage;
using LibraHub.Content.Application;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Infrastructure.Clients;
using LibraHub.Content.Infrastructure.Persistence;
using LibraHub.Content.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;

namespace LibraHub.Content.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContentDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ContentDb")
            ?? throw new InvalidOperationException("Connection string 'ContentDb' not found.");

        services.AddDbContext<ContentDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                })
                   .UseLazyLoadingProxies());

        return services;
    }

    public static IServiceCollection AddContentApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        services.AddScoped<IStoredObjectRepository, StoredObjectRepository>();
        services.AddScoped<IBookEditionRepository, BookEditionRepository>();
        services.AddScoped<ICoverRepository, CoverRepository>();
        services.AddScoped<IAccessGrantRepository, AccessGrantRepository>();

        services.AddScoped<IOutboxWriter, OutboxEventPublisher<ContentDbContext>>();
        services.AddScoped<IUnitOfWork, BuildingBlocks.Persistence.UnitOfWork<ContentDbContext>>();
        services.AddScoped<IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, BuildingBlocks.CurrentUser.CurrentUser>();

        services.AddOptions<StorageOptions>().Bind(configuration.GetSection(StorageOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddSingleton<MinioClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var client = new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey);

            if (options.UseSsl)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        services.AddScoped<IObjectStorage, MinioObjectStorage>();

        services.AddOptions<UploadOptions>().Bind(configuration.GetSection(UploadOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ReadAccessOptions>().Bind(configuration.GetSection(ReadAccessOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddHttpClient<ICatalogReadClient, CatalogReadClient>();
        services.AddHttpClient<ILibraryAccessClient, LibraryAccessClient>();

        return services;
    }

    public static IServiceCollection AddContentJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubJwtAuthentication(configuration);
    }

    public static IServiceCollection AddContentRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubRabbitMq<OutboxPublisherWorkerHelper<ContentDbContext>>(configuration);
    }

    public static IServiceCollection AddContentHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubHealthChecks(configuration, "ContentDb");
    }
}
