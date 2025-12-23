namespace LibraHub.Identity.Infrastructure.Options;

public class TokenOptions
{
    public const string SectionName = "Tokens";

    public int PasswordResetExpirationHours { get; set; } = 24;
    public int RegistrationCompletionExpirationHours { get; set; } = 72; // 3 days
    public int EmailVerificationExpirationDays { get; set; } = 7;
}

