using LibraHub.Notifications.Domain.Notifications;
using LibraHub.Notifications.Domain.Preferences;

namespace LibraHub.Notifications.Application.Abstractions;

public interface INotificationPreferencesRepository
{
    Task<NotificationPreference?> GetByUserIdAndTypeAsync(Guid userId, NotificationType type, CancellationToken cancellationToken = default);

    Task<List<NotificationPreference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetUserIdsWithEnabledNotificationsAsync(NotificationType type, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetUserIdsWithInAppEnabledAsync(NotificationType type, CancellationToken cancellationToken = default);

    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

    Task UpdateAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

    Task DeleteAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
}
