using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Commands.MarkAsRead;

public record MarkAsReadCommand(List<Guid> NotificationIds) : IRequest<Result>;
