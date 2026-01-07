using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsHandler(
    INotificationRepository notificationRepository,
    ICurrentUser currentUser) : IRequestHandler<GetMyNotificationsQuery, GetMyNotificationsDto>
{
    public async Task<GetMyNotificationsDto> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(NotificationsErrors.User.NotAuthenticated);
        }

        var userId = currentUser.UserId.Value;
        var notifications = await notificationRepository.GetByUserIdAsync(userId, request.Skip, request.Take, cancellationToken);
        var totalCount = await notificationRepository.GetTotalCountByUserIdAsync(userId, cancellationToken);

        var notificationDtos = notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type.ToString(),
            Title = n.Title,
            Message = n.Message,
            Status = n.Status.ToString(),
            CreatedAt = new DateTimeOffset(n.CreatedAt, TimeSpan.Zero),
            ReadAt = n.ReadAt.HasValue ? new DateTimeOffset(n.ReadAt.Value, TimeSpan.Zero) : null
        }).ToList();

        return new GetMyNotificationsDto
        {
            Notifications = notificationDtos,
            TotalCount = totalCount
        };
    }
}
