namespace LibraHub.Identity.Api.Dtos.Users;

public record CompleteRegistrationRequestDto
{
    public string Token { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateTimeOffset DateOfBirth { get; init; }
    public bool EmailAnnouncementsEnabled { get; init; }
    public bool EmailPromotionsEnabled { get; init; }
}
