using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser) : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(NotificationsErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure<int>(userIdResult.Error!);
        }

        var userId = userIdResult.Value;
        var count = await notificationRepository.GetUnreadCountByUserIdAsync(userId, cancellationToken);
        return Result.Success(count);
    }
}
