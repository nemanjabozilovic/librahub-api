using LibraHub.BuildingBlocks.Persistence;

namespace LibraHub.Notifications.Infrastructure.Persistence;

public class UnitOfWork(NotificationsDbContext context) : UnitOfWork<NotificationsDbContext>(context)
{
}
