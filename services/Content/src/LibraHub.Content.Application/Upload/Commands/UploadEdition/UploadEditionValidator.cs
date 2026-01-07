using FluentValidation;
using LibraHub.Content.Application.Options;
using Microsoft.Extensions.Options;

namespace LibraHub.Content.Application.Upload.Commands.UploadEdition;

public class UploadEditionValidator : AbstractValidator<UploadEditionCommand>
{
    private static readonly Dictionary<string, string[]> AllowedFormats = new()
    {
        { "pdf", new[] { "application/pdf" } },
        { "epub", new[] { "application/epub+zip", "application/epub" } }
    };

    public UploadEditionValidator(IOptions<UploadOptions> uploadOptions)
    {
        var maxFileSizeBytes = uploadOptions.Value.MaxEditionSizeBytes;

        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required");

        RuleFor(x => x.Format)
            .NotEmpty()
            .WithMessage("Format is required")
            .Must(format => AllowedFormats.ContainsKey(format.ToLowerInvariant()))
            .WithMessage($"Format must be one of: {string.Join(", ", AllowedFormats.Keys)}");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required")
            .Must(file => file.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(file => file.Length <= maxFileSizeBytes)
            .WithMessage($"File size must not exceed {maxFileSizeBytes / (1024 * 1024)} MB")
            .Must((command, file) => IsValidContentType(command.Format, file.ContentType))
            .WithMessage("File content type does not match the specified format");
    }

    private static bool IsValidContentType(string format, string contentType)
    {
        if (!AllowedFormats.TryGetValue(format.ToLowerInvariant(), out var allowedTypes))
        {
            return false;
        }

        return allowedTypes.Contains(contentType.ToLowerInvariant());
    }
}
