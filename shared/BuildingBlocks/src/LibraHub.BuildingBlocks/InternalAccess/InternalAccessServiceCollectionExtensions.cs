using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LibraHub.BuildingBlocks.InternalAccess;

public static class InternalAccessServiceCollectionExtensions
{
    public static IServiceCollection AddLibraHubInternalAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InternalAccessOptions>()
            .Bind(configuration.GetSection(InternalAccessOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<InternalAccessHeaderHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(InternalAccessConstants.PolicyName, policy =>
            {
                policy.RequireAssertion(ctx =>
                {
                    var httpContext = GetHttpContext(ctx.Resource);
                    if (httpContext == null)
                    {
                        return false;
                    }

                    if (!httpContext.Request.Headers.TryGetValue(InternalAccessConstants.HeaderName, out var provided))
                    {
                        return false;
                    }

                    var expected = httpContext.RequestServices
                        .GetRequiredService<IOptions<InternalAccessOptions>>()
                        .Value.ApiKey;

                    return !string.IsNullOrWhiteSpace(expected) &&
                           string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
                });
            });
        });

        return services;
    }

    private static HttpContext? GetHttpContext(object? resource)
    {
        return resource switch
        {
            HttpContext httpContext => httpContext,
            AuthorizationFilterContext mvc => mvc.HttpContext,
            _ => null
        };
    }
}

