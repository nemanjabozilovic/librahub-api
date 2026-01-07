using System.ComponentModel.DataAnnotations;

namespace LibraHub.BuildingBlocks.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(ErrorMessage = "Storage Endpoint is required")]
    public string Endpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "Storage AccessKey is required")]
    public string AccessKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Storage SecretKey is required")]
    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; }
}
