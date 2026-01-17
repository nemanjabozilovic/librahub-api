namespace LibraHub.Identity.Application.Me.Queries.GetMyProfile;

public record UserProfileDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTimeOffset DateOfBirth { get; init; }
    public string? Phone { get; init; }
    public string? Avatar { get; init; }
    public bool EmailAnnouncementsEnabled { get; init; }
    public bool EmailPromotionsEnabled { get; init; }
}
