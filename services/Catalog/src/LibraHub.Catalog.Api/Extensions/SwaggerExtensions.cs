using LibraHub.BuildingBlocks.Swagger;

namespace LibraHub.Catalog.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCatalogSwagger(this IServiceCollection services)
    {
        return services.AddLibraHubSwagger(
            "LibraHub Catalog API",
            "v1",
            "Catalog service API for managing books and announcements");
    }

    public static IApplicationBuilder UseCatalogSwagger(this IApplicationBuilder app)
    {
        return app.UseLibraHubSwagger("LibraHub Catalog API");
    }
}
