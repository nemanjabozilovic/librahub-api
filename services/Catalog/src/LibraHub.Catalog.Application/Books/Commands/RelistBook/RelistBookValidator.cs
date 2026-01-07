using FluentValidation;

namespace LibraHub.Catalog.Application.Books.Commands.RelistBook;

public class RelistBookValidator : AbstractValidator<RelistBookCommand>
{
    public RelistBookValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required");
    }
}
