using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibraHub.Notifications.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=librahub_notifications;Username=librahub_admin;Password=L1br@Hub_DB_2026!S3cur3_P@ss");

        return new NotificationsDbContext(optionsBuilder.Options);
    }
}

