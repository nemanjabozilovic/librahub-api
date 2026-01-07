using FluentValidation;

namespace LibraHub.Content.Application.Access.Commands.CreateReadToken;

public class CreateReadTokenValidator : AbstractValidator<CreateReadTokenCommand>
{
    private static readonly string[] AllowedFormats = { "pdf", "epub" };

    public CreateReadTokenValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required");

        RuleFor(x => x.Format)
            .Must(format => string.IsNullOrEmpty(format) || AllowedFormats.Contains(format?.ToLowerInvariant()))
            .WithMessage($"Format must be one of: {string.Join(", ", AllowedFormats)}")
            .When(x => !string.IsNullOrEmpty(x.Format));

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .When(x => x.Version.HasValue)
            .WithMessage("Version must be greater than 0");
    }
}
