using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Notifications.Application.Notifications.Commands.MarkAsRead;

public class MarkAsReadHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser) : IRequestHandler<MarkAsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(NotificationsErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure(userIdResult.Error!);
        }

        var currentUserId = userIdResult.Value;

        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null)
        {
            return Result.Failure(Error.NotFound(NotificationsErrors.Notification.NotFound));
        }

        if (notification.UserId != currentUserId)
        {
            return Result.Failure(Error.Forbidden("Notification does not belong to current user"));
        }

        notification.MarkAsRead();
        await notificationRepository.UpdateAsync(notification, cancellationToken);

        return Result.Success();
    }
}
