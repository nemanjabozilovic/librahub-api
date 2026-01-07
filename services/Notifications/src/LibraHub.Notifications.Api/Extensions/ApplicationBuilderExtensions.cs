using LibraHub.Notifications.Api.Hubs;
using LibraHub.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Notifications.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseNotificationsDatabaseMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        context.Database.Migrate();
        return app;
    }

    public static IEndpointRouteBuilder MapNotificationsSignalRHub(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        var signalRHubPath = configuration["Notifications:SignalRHubPath"]
            ?? throw new InvalidOperationException("Notifications:SignalRHubPath configuration is missing.");

        app.MapHub<NotificationsHub>(signalRHubPath);

        return app;
    }
}
