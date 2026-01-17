using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Notifications.Application.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(int Skip = 0, int Take = 20) : IRequest<Result<GetMyNotificationsDto>>;
