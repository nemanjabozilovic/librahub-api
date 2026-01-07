using FluentValidation;
using LibraHub.Content.Application.Options;
using Microsoft.Extensions.Options;

namespace LibraHub.Content.Application.Upload.Commands.UploadCover;

public class UploadCoverValidator : AbstractValidator<UploadCoverCommand>
{
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    public UploadCoverValidator(IOptions<UploadOptions> uploadOptions)
    {
        var maxFileSizeBytes = uploadOptions.Value.MaxCoverSizeBytes;

        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required")
            .Must(file => file.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(file => file.Length <= maxFileSizeBytes)
            .WithMessage($"File size must not exceed {maxFileSizeBytes / (1024 * 1024)} MB")
            .Must(file => AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}");
    }
}
