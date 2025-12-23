using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser) : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(NotificationsErrors.User.NotAuthenticated);
        }

        var userId = currentUser.UserId.Value;
        return await notificationRepository.GetUnreadCountByUserIdAsync(userId, cancellationToken);
    }
}

