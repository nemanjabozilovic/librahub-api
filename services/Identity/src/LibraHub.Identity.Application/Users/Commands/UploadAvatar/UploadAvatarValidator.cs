using FluentValidation;

namespace LibraHub.Identity.Application.Users.Commands.UploadAvatar;

public class UploadAvatarValidator : AbstractValidator<UploadAvatarCommand>
{
    public UploadAvatarValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(f => f != null && f.Length > 0).WithMessage("File cannot be empty")
            .Must(f => f != null && f.Length <= 5 * 1024 * 1024).WithMessage("File size must not exceed 5MB")
            .Must(f => f != null && new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }
                .Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
            .WithMessage("Allowed file extensions: .jpg, .jpeg, .png, .gif, .webp");
    }
}

