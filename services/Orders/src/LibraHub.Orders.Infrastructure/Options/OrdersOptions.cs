using System.ComponentModel.DataAnnotations;

namespace LibraHub.Orders.Infrastructure.Options;

public class OrdersOptions
{
    public const string SectionName = "Orders";

    [Required(ErrorMessage = "CatalogApiUrl is required")]
    [Url(ErrorMessage = "CatalogApiUrl must be a valid URL")]
    public string CatalogApiUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "LibraryApiUrl is required")]
    [Url(ErrorMessage = "LibraryApiUrl must be a valid URL")]
    public string LibraryApiUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "IdentityApiUrl is required")]
    [Url(ErrorMessage = "IdentityApiUrl must be a valid URL")]
    public string IdentityApiUrl { get; set; } = string.Empty;
}
