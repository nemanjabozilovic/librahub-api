using FluentValidation;

namespace LibraHub.Catalog.Application.Books.Commands.UpdateBook;

public class UpdateBookValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required");

        RuleFor(x => x.Title)
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Language)
            .MaximumLength(50).WithMessage("Language must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Language));

        RuleFor(x => x.Publisher)
            .MaximumLength(200).WithMessage("Publisher must not exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Publisher));
    }
}
