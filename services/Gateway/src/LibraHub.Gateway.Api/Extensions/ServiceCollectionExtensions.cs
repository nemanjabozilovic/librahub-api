using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Swagger;
using LibraHub.Gateway.Api.Options;

namespace LibraHub.Gateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewaySwagger(this IServiceCollection services)
    {
        return services.AddLibraHubSwagger(
            "LibraHub Gateway API",
            "v1",
            "API Gateway for LibraHub microservices architecture");
    }

    public static IServiceCollection AddGatewayJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLibraHubJwtAuthentication(configuration);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AllowAnonymous", policy =>
            {
                policy.RequireAssertion(_ => true);
            });

            options.AddPolicy("RequireAuthenticated", policy =>
            {
                policy.RequireAuthenticatedUser();
            });

            options.AddPolicy("RequireAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            options.AddPolicy("RequireLibrarian", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Librarian", "Admin");
            });
        });

        return services;
    }

    public static IServiceCollection AddGatewayCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOptions = new GatewayCorsOptions();
        configuration.GetSection(GatewayCorsOptions.SectionName).Bind(corsOptions);

        if (corsOptions.AllowedOrigins == null || corsOptions.AllowedOrigins.Count == 0)
        {
            throw new InvalidOperationException($"{GatewayCorsOptions.SectionName}:{nameof(GatewayCorsOptions.AllowedOrigins)} must contain at least one origin.");
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsOptions.AllowedOrigins.ToArray())
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddGatewayReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }
}
