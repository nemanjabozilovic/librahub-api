using LibraHub.BuildingBlocks.Swagger;

namespace LibraHub.Identity.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddIdentitySwagger(this IServiceCollection services)
    {
        return services.AddLibraHubSwagger("LibraHub Identity API");
    }

    public static IApplicationBuilder UseIdentitySwagger(this IApplicationBuilder app)
    {
        return app.UseLibraHubSwagger("LibraHub Identity API");
    }
}
