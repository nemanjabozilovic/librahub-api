using System.ComponentModel.DataAnnotations;

namespace LibraHub.Content.Application.Options;

public class UploadOptions
{
    public const string SectionName = "Upload";

    [Range(1, long.MaxValue, ErrorMessage = "MaxCoverSizeBytes must be greater than 0")]
    public long MaxCoverSizeBytes { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "MaxEditionSizeBytes must be greater than 0")]
    public long MaxEditionSizeBytes { get; set; }

    [Required(ErrorMessage = "CoversBucketName is required")]
    public string CoversBucketName { get; set; } = string.Empty;

    [Required(ErrorMessage = "EditionsBucketName is required")]
    public string EditionsBucketName { get; set; } = string.Empty;
}
