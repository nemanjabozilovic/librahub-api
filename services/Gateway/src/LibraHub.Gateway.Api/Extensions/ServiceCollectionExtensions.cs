using LibraHub.BuildingBlocks.Auth;
using LibraHub.BuildingBlocks.Swagger;

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

    public static IServiceCollection AddGatewayReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }
}
