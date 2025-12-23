using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Commands.MarkAsRead;

public class MarkAsReadHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser) : IRequestHandler<MarkAsReadCommand>
{
    public async Task Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(NotificationsErrors.User.NotAuthenticated);
        }

        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null)
        {
            throw new InvalidOperationException(NotificationsErrors.Notification.NotFound);
        }

        if (notification.UserId != currentUser.UserId.Value)
        {
            throw new UnauthorizedAccessException("Notification does not belong to current user");
        }

        notification.MarkAsRead();
        await notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}

