using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser) : IRequestHandler<GetMyNotificationsQuery, Result<GetMyNotificationsDto>>
{
    public async Task<Result<GetMyNotificationsDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(NotificationsErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure<GetMyNotificationsDto>(userIdResult.Error!);
        }

        var userId = userIdResult.Value;
        var notifications = await notificationRepository.GetByUserIdAsync(userId, request.Skip, request.Take, cancellationToken);
        var totalCount = await notificationRepository.GetTotalCountByUserIdAsync(userId, cancellationToken);

        var notificationDtos = notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type.ToString(),
            Title = n.Title,
            Message = n.Message,
            ImageUrl = n.ImageUrl,
            Status = n.Status.ToString(),
            CreatedAt = new DateTimeOffset(n.CreatedAt, TimeSpan.Zero),
            ReadAt = n.ReadAt.HasValue ? new DateTimeOffset(n.ReadAt.Value, TimeSpan.Zero) : null
        }).ToList();

        return Result.Success(new GetMyNotificationsDto
        {
            Notifications = notificationDtos,
            TotalCount = totalCount
        });
    }
}
