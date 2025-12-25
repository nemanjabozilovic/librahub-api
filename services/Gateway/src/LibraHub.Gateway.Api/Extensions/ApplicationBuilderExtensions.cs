using LibraHub.BuildingBlocks.Swagger;

namespace LibraHub.Gateway.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGatewaySwagger(this IApplicationBuilder app)
    {
        return app.UseLibraHubSwagger("LibraHub Gateway API", "v1");
    }
}

