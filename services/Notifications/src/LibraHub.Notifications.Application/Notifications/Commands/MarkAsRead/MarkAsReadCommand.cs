using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Commands.MarkAsRead;

public record MarkAsReadCommand(Guid NotificationId) : IRequest<Result>;
