using FluentValidation;

namespace LibraHub.Identity.Application.Auth.Commands.ResendVerificationEmail;

public class ResendVerificationEmailValidator : AbstractValidator<ResendVerificationEmailCommand>
{
    public ResendVerificationEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
