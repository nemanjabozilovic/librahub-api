using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Api.Dtos.Users;

public record UpdateUserRequestDto
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name must not exceed 100 characters")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name must not exceed 100 characters")]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Phone must not exceed 20 characters")]
    public string? Phone { get; init; }

    public DateTime? DateOfBirth { get; init; }

    public bool? EmailVerified { get; init; }
}

