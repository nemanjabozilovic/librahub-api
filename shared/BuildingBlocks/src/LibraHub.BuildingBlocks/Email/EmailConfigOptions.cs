using System.ComponentModel.DataAnnotations;

namespace LibraHub.BuildingBlocks.Email;

public class EmailConfigOptions
{
    public const string SectionName = "EmailConfig";

    [Required(ErrorMessage = "Email Host is required")]
    public string Host { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Port is required")]
    [Range(1, 65535, ErrorMessage = "Email Port must be between 1 and 65535")]
    public int Port { get; set; }

    [Required(ErrorMessage = "Email From address is required")]
    [EmailAddress(ErrorMessage = "Email From must be a valid email address")]
    public string From { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool EnableSsl { get; set; }
}
