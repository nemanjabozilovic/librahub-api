using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Recipients;
using LibraHub.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Notifications.Infrastructure.Repositories;

public class UserNotificationSettingsRepository(NotificationsDbContext context) : IUserNotificationSettingsRepository
{
    public async Task UpsertAsync(UserNotificationSettings settings, CancellationToken cancellationToken = default)
    {
        var existing = await context.UserNotificationSettings
            .FirstOrDefaultAsync(x => x.UserId == settings.UserId, cancellationToken);

        if (existing == null)
        {
            await context.UserNotificationSettings.AddAsync(settings, cancellationToken);
        }
        else
        {
            existing.UpdateEmail(settings.Email, settings.IsActive, settings.IsStaff);
            existing.Update(settings.EmailEnabled, inAppEnabled: true);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserNotificationSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.UserNotificationSettings
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<List<UserNotificationSettings>> GetEmailRecipientsAsync(CancellationToken cancellationToken = default)
    {
        return await context.UserNotificationSettings
            .Where(x =>
                x.IsActive &&
                !x.IsStaff &&
                x.EmailEnabled &&
                !string.IsNullOrWhiteSpace(x.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetActiveNonStaffUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await context.UserNotificationSettings
            .Where(x => x.IsActive && !x.IsStaff)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetActiveNonStaffUserIdsWithInAppEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await context.UserNotificationSettings
            .Where(x => x.IsActive && !x.IsStaff && x.InAppEnabled)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<Guid>> GetStaffUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await context.UserNotificationSettings
            .Where(x => x.IsStaff)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await context.UserNotificationSettings
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (existing == null)
        {
            return;
        }

        context.UserNotificationSettings.Remove(existing);

        if (context.Database.CurrentTransaction == null)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
