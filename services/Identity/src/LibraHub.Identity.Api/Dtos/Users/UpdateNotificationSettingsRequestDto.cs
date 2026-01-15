namespace LibraHub.Identity.Api.Dtos.Users;

public record UpdateNotificationSettingsRequestDto
{
    public bool? EmailAnnouncementsEnabled { get; init; }
    public bool? EmailPromotionsEnabled { get; init; }
}

