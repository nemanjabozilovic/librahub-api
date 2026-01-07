using System.ComponentModel.DataAnnotations;

namespace LibraHub.Catalog.Application.Options;

public class CatalogOptions
{
    public const string SectionName = "Catalog";

    [Required(ErrorMessage = "GatewayBaseUrl is required")]
    [Url(ErrorMessage = "GatewayBaseUrl must be a valid URL")]
    public string GatewayBaseUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "ContentApiUrl is required")]
    [Url(ErrorMessage = "ContentApiUrl must be a valid URL")]
    public string ContentApiUrl { get; set; } = string.Empty;
}
