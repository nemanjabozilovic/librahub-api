using LibraHub.Content.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Content.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseContentDatabaseMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        context.Database.Migrate();

        return app;
    }
}
