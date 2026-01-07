using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Api.Dtos.Users;

public record CreateUserRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; init; } = string.Empty;
}
