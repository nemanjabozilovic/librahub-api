namespace LibraHub.Identity.Application.Me.Queries.GetMe;

public record GetMeResponseDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Avatar { get; init; }
    public DateTimeOffset? DateOfBirth { get; init; }
    public List<string> Roles { get; init; } = new();
    public bool EmailVerified { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}
