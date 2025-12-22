using FluentValidation;

namespace LibraHub.Catalog.Application.Books.Commands.PublishBook;

public class PublishBookValidator : AbstractValidator<PublishBookCommand>
{
    public PublishBookValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required");
    }
}
