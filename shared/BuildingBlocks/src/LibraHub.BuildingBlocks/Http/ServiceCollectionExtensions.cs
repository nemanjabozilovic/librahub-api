using Microsoft.Extensions.DependencyInjection;

namespace LibraHub.BuildingBlocks.Http;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceClientHelper(this IServiceCollection services)
    {
        services.AddHttpClient<ServiceClientHelper>();
        return services;
    }
}
