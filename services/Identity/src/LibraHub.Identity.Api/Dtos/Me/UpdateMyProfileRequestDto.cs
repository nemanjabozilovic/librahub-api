using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Api.Dtos.Me;

public record UpdateMyProfileRequestDto
{
    [Required(ErrorMessage = "FirstName is required")]
    [MaxLength(100, ErrorMessage = "FirstName must not exceed 100 characters")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "LastName is required")]
    [MaxLength(100, ErrorMessage = "LastName must not exceed 100 characters")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "DateOfBirth is required")]
    public DateTimeOffset DateOfBirth { get; init; }

    [MaxLength(20, ErrorMessage = "Phone must not exceed 20 characters")]
    public string? Phone { get; init; }

    public bool EmailAnnouncementsEnabled { get; init; }
    public bool EmailPromotionsEnabled { get; init; }
}

