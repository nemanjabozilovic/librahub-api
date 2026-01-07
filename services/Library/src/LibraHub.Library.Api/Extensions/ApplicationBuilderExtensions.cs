using LibraHub.Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Library.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLibraryDatabaseMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        context.Database.Migrate();

        return app;
    }
}
