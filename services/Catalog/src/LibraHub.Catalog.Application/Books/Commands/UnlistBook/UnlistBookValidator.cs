using FluentValidation;

namespace LibraHub.Catalog.Application.Books.Commands.UnlistBook;

public class UnlistBookValidator : AbstractValidator<UnlistBookCommand>
{
    public UnlistBookValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required");
    }
}
