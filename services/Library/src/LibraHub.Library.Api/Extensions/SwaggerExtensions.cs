using LibraHub.BuildingBlocks.Swagger;

namespace LibraHub.Library.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddLibrarySwagger(this IServiceCollection services)
    {
        return services.AddLibraHubSwagger(
            "LibraHub Library API",
            "v1",
            "Library service API for managing user entitlements and reading progress");
    }

    public static IApplicationBuilder UseLibrarySwagger(this IApplicationBuilder app)
    {
        return app.UseLibraHubSwagger("LibraHub Library API");
    }
}
