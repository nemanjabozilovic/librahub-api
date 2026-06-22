using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Idempotency;
using LibraHub.BuildingBlocks.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace LibraHub.BuildingBlocks.Hosting;

public static class WebApplicationExtensions
{
    public static WebApplication UseLibraHubPipeline(this WebApplication app, bool useIdempotency = false)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<CorrelationIdMiddleware>();

        if (useIdempotency)
        {
            app.UseMiddleware<IdempotencyKeyMiddleware>();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
