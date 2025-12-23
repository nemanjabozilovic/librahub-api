using LibraHub.Notifications.Domain.Notifications;

namespace LibraHub.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
}

