using FluentValidation;

namespace LibraHub.Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.BookIds)
            .NotEmpty()
            .WithMessage("At least one book must be specified")
            .Must(ids => ids.Count <= 50)
            .WithMessage("Cannot order more than 50 books at once")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate book IDs are not allowed");
    }
}
