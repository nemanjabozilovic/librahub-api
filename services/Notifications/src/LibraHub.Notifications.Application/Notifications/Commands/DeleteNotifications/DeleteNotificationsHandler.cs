using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Notifications.Application.Notifications.Commands.DeleteNotifications;

public class DeleteNotificationsHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteNotificationsCommand, Result>
{
    public async Task<Result> Handle(DeleteNotificationsCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(NotificationsErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure(userIdResult.Error!);
        }

        var currentUserId = userIdResult.Value;

        if (request.NotificationIds == null || request.NotificationIds.Count == 0)
        {
            return Result.Failure(Error.Validation("At least one notification ID is required"));
        }

        var notifications = new List<Domain.Notifications.Notification>();

        foreach (var notificationId in request.NotificationIds)
        {
            var notification = await notificationRepository.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
            {
                return Result.Failure(Error.NotFound($"Notification with ID {notificationId} not found"));
            }

            if (notification.UserId != currentUserId)
            {
                return Result.Failure(Error.Forbidden("Notification does not belong to current user"));
            }

            notifications.Add(notification);
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await notificationRepository.DeleteRangeAsync(notifications, ct);
        }, cancellationToken);

        return Result.Success();
    }
}
