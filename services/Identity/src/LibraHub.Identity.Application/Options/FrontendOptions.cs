using System.ComponentModel.DataAnnotations;

namespace LibraHub.Identity.Application.Options;

public class FrontendOptions
{
    public const string SectionName = "Frontend";

    [Required(ErrorMessage = "Frontend BaseUrl is required")]
    [Url(ErrorMessage = "Frontend BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;
}
