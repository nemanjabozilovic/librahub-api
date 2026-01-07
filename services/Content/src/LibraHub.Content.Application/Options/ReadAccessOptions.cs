using System.ComponentModel.DataAnnotations;

namespace LibraHub.Content.Application.Options;

public class ReadAccessOptions
{
    public const string SectionName = "ReadAccess";

    [Required(ErrorMessage = "CatalogApiUrl is required")]
    [Url(ErrorMessage = "CatalogApiUrl must be a valid URL")]
    public string CatalogApiUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "LibraryApiUrl is required")]
    [Url(ErrorMessage = "LibraryApiUrl must be a valid URL")]
    public string LibraryApiUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "TokenExpirationMinutes must be greater than 0")]
    public int TokenExpirationMinutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "TokenRefreshThresholdMinutes must be greater than 0")]
    public int TokenRefreshThresholdMinutes { get; set; } = 5;
}
