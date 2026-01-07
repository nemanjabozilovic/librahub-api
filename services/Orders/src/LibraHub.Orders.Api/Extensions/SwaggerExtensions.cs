using LibraHub.BuildingBlocks.Swagger;

namespace LibraHub.Orders.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddOrdersSwagger(this IServiceCollection services)
    {
        return services.AddLibraHubSwagger(
            "LibraHub Orders API",
            "v1",
            "Orders service API for managing book orders and payments");
    }

    public static IApplicationBuilder UseOrdersSwagger(this IApplicationBuilder app)
    {
        return app.UseLibraHubSwagger("LibraHub Orders API");
    }
}
