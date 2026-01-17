namespace LibraHub.Contracts.Identity.V1;

public record UserNotificationSettingsChangedV1
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsStaff { get; init; }
    public bool EmailAnnouncementsEnabled { get; init; }
    public bool EmailPromotionsEnabled { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
