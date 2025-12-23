using LibraHub.BuildingBlocks.Swagger;

namespace LibraHub.Notifications.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddNotificationsSwagger(this IServiceCollection services)
    {
        return services.AddLibraHubSwagger(
            "LibraHub Notifications API",
            "v1",
            "API for managing user notifications");
    }

    public static IApplicationBuilder UseNotificationsSwagger(this IApplicationBuilder app)
    {
        return app.UseLibraHubSwagger("LibraHub Notifications API");
    }
}

