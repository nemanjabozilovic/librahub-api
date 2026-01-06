using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Api.Dtos.Users;

public class GetUsersByIdsRequestDto
{
    [Required(ErrorMessage = "UserIds list is required")]
    [MinLength(1, ErrorMessage = "At least one UserId is required")]
    [MaxLength(100, ErrorMessage = "Maximum 100 UserIds allowed")]
    public List<Guid> UserIds { get; set; } = new();
}

