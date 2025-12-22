using LibraHub.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Catalog.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCatalogDatabaseMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        context.Database.Migrate();
        return app;
    }
}
