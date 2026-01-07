using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Infrastructure.Options;

public class TokenOptions
{
    public const string SectionName = "Tokens";

    [Range(1, int.MaxValue, ErrorMessage = "PasswordResetExpirationHours must be greater than 0")]
    public int PasswordResetExpirationHours { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RegistrationCompletionExpirationHours must be greater than 0")]
    public int RegistrationCompletionExpirationHours { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "EmailVerificationExpirationDays must be greater than 0")]
    public int EmailVerificationExpirationDays { get; set; }
}
