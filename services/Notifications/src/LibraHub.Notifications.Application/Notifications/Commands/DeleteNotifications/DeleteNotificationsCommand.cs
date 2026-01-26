using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Commands.DeleteNotifications;

public record DeleteNotificationsCommand(List<Guid> NotificationIds) : IRequest<Result>;
