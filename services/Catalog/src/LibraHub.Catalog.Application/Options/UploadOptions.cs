using System.ComponentModel.DataAnnotations;

namespace LibraHub.Catalog.Application.Options;

public class UploadOptions
{
    public const string SectionName = "Upload";

    [Range(1, long.MaxValue, ErrorMessage = "MaxAnnouncementImageSizeBytes must be greater than 0")]
    public long MaxAnnouncementImageSizeBytes { get; set; } = 10485760;

    [Required(ErrorMessage = "AnnouncementImagesBucketName is required")]
    public string AnnouncementImagesBucketName { get; set; } = "announcement-images";
}
