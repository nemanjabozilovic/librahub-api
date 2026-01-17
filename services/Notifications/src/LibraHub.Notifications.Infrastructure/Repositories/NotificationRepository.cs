using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Notifications;
using LibraHub.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Notifications.Infrastructure.Repositories;

public class NotificationRepository(NotificationsDbContext context) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<List<Notification>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId, cancellationToken);
    }

    public async Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && n.Status == NotificationStatus.Unread, cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await context.Notifications.AddAsync(notification, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        await context.Notifications.AddRangeAsync(notifications, cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        context.Notifications.Update(notification);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        context.Notifications.Remove(notification);

        if (context.Database.CurrentTransaction == null)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
