using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Application.Options;

public class IdentityOptions
{
    public const string SectionName = "Identity";

    [Required(ErrorMessage = "GatewayBaseUrl is required")]
    [Url(ErrorMessage = "GatewayBaseUrl must be a valid URL")]
    public string GatewayBaseUrl { get; set; } = string.Empty;
}
