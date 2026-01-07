using System.ComponentModel.DataAnnotations;

namespace LibraHub.Notifications.Infrastructure.Options;

public class NotificationsOptions
{
    public const string SectionName = "Notifications";

    [Required(ErrorMessage = "IdentityApiUrl is required")]
    [Url(ErrorMessage = "IdentityApiUrl must be a valid URL")]
    public string IdentityApiUrl { get; set; } = string.Empty;
}
