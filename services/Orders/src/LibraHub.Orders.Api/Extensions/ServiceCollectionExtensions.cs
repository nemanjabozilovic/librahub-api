using FluentValidation;
using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Health;
using LibraHub.BuildingBlocks.Idempotency;
using LibraHub.BuildingBlocks.Messaging;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.Orders.Application;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Infrastructure.Clients;
using LibraHub.Orders.Infrastructure.Idempotency;
using LibraHub.Orders.Infrastructure.Options;
using LibraHub.Orders.Infrastructure.Payments;
using LibraHub.Orders.Infrastructure.Persistence;
using LibraHub.Orders.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Orders.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrdersDb")
            ?? throw new InvalidOperationException("Connection string 'OrdersDb' not found.");

        services.AddDbContext<OrdersDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                }));

        return services;
    }

    public static IServiceCollection AddOrdersApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();

        services.AddScoped<Application.Consumers.OrderRefundedConsumer>();

        services.AddScoped<BuildingBlocks.Abstractions.IOutboxWriter, OutboxEventPublisher<OrdersDbContext>>();
        services.AddScoped<BuildingBlocks.Abstractions.IUnitOfWork, UnitOfWork>();
        services.AddScoped<BuildingBlocks.Abstractions.IClock, BuildingBlocks.Clock>();
        services.AddHttpContextAccessor();
        services.AddScoped<BuildingBlocks.Abstractions.ICurrentUser, BuildingBlocks.CurrentUser.CurrentUser>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore<OrdersDbContext, IdempotencyKey>>();

        services.AddOptions<MockPaymentOptions>().Bind(configuration.GetSection(MockPaymentOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OrdersOptions>().Bind(configuration.GetSection(OrdersOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddScoped<IPaymentGateway, MockPaymentGateway>();

        services.AddHttpClient<ICatalogPricingClient, CatalogPricingClient>();
        services.AddHttpClient<ILibraryOwnershipClient, LibraryOwnershipClient>();
        services.AddHttpClient<IIdentityClient, IdentityClient>();

        services.AddRedisCache(configuration);

        return services;
    }

    public static IServiceCollection AddOrdersJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubJwtAuthentication(configuration);
    }

    public static IServiceCollection AddOrdersRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubRabbitMq<OutboxPublisherWorkerHelper<OrdersDbContext>>(configuration);
    }

    public static IServiceCollection AddOrdersHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLibraHubHealthChecks(configuration, "OrdersDb");
    }
}

