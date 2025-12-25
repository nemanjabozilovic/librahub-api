namespace LibraHub.Identity.Application.Users.Queries.GetUsers;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public bool EmailVerified { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

