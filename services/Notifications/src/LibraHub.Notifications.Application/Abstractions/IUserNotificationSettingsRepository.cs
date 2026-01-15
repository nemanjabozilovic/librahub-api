using LibraHub.Notifications.Domain.Recipients;

namespace LibraHub.Notifications.Application.Abstractions;

public interface IUserNotificationSettingsRepository
{
    Task UpsertAsync(UserNotificationSettings settings, CancellationToken cancellationToken = default);
    Task<List<UserNotificationSettings>> GetEmailAnnouncementRecipientsAsync(CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetStaffUserIdsAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}

