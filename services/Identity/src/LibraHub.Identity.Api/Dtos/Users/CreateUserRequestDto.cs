namespace LibraHub.Identity.Api.Dtos.Users;

public record CreateUserRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "User";
}

