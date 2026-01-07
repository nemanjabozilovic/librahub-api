namespace LibraHub.Identity.Application.Auth.Dtos;

public record RegisterRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateTimeOffset DateOfBirth { get; init; }
}
