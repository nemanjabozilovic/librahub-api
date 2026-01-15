using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Notifications;
using LibraHub.Notifications.Domain.Preferences;
using LibraHub.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Notifications.Infrastructure.Repositories;

public class NotificationPreferencesRepository : INotificationPreferencesRepository
{
    private readonly NotificationsDbContext _context;

    public NotificationPreferencesRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationPreference?> GetByUserIdAndTypeAsync(Guid userId, NotificationType type, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type, cancellationToken);
    }

    public async Task<List<NotificationPreference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetUserIdsWithEnabledNotificationsAsync(NotificationType type, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationPreferences
            .Where(p => p.Type == type && (p.EmailEnabled || p.InAppEnabled))
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetUserIdsWithInAppEnabledAsync(NotificationType type, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationPreferences
            .Where(p => p.Type == type && p.InAppEnabled)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        await _context.NotificationPreferences.AddAsync(preference, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        _context.NotificationPreferences.Update(preference);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        _context.NotificationPreferences.Remove(preference);

        if (_context.Database.CurrentTransaction == null)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
