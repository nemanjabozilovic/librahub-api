using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery : IRequest<int>;
