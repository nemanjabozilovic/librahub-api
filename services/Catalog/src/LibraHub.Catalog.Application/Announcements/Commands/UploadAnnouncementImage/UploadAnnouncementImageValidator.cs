using FluentValidation;
using Microsoft.Extensions.Options;

namespace LibraHub.Catalog.Application.Announcements.Commands.UploadAnnouncementImage;

public class UploadAnnouncementImageValidator : AbstractValidator<UploadAnnouncementImageCommand>
{
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };

    public UploadAnnouncementImageValidator(IOptions<Options.UploadOptions> uploadOptions)
    {
        RuleFor(x => x.AnnouncementId)
            .NotEmpty().WithMessage("Announcement ID is required");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(f => f != null && f.Length > 0).WithMessage("File cannot be empty")
            .Must(f => f != null && f.Length <= uploadOptions.Value.MaxAnnouncementImageSizeBytes)
            .WithMessage($"File size must not exceed {uploadOptions.Value.MaxAnnouncementImageSizeBytes / (1024 * 1024)} MB")
            .Must(f => f != null && AllowedContentTypes.Contains(f.ContentType.ToLowerInvariant()))
            .WithMessage("File must be a valid image (JPEG, PNG, WebP, or GIF)");
    }
}
