using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Application.Options;

public class SecurityOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "MaxFailedLoginAttempts must be greater than 0")]
    public int MaxFailedLoginAttempts { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "LockoutDurationMinutes must be greater than 0")]
    public int LockoutDurationMinutes { get; set; }
}
