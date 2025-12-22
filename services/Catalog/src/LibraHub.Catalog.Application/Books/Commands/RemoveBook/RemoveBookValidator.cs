using FluentValidation;

namespace LibraHub.Catalog.Application.Books.Commands.RemoveBook;

public class RemoveBookValidator : AbstractValidator<RemoveBookCommand>
{
    public RemoveBookValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Removal reason is required")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters");
    }
}
