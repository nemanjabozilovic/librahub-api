using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Commands.UpdateNotificationSettings;

public record UpdateNotificationSettingsCommand(
    bool? EmailAnnouncementsEnabled,
    bool? EmailPromotionsEnabled) : IRequest<Result>;

