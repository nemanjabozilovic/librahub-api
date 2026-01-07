using LibraHub.BuildingBlocks.Swagger;

namespace LibraHub.Content.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddContentSwagger(this IServiceCollection services)
    {
        return services.AddLibraHubSwagger(
            "LibraHub Content API",
            "v1",
            "Content service API for managing book content (covers and editions)");
    }

    public static IApplicationBuilder UseContentSwagger(this IApplicationBuilder app)
    {
        return app.UseLibraHubSwagger("LibraHub Content API");
    }
}
