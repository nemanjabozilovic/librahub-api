using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Infrastructure.Options;

public class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    [Required(ErrorMessage = "SecretKey is required")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "VerifyUrl is required")]
    public string VerifyUrl { get; set; } = string.Empty;
}
