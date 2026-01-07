using FluentValidation;

namespace LibraHub.Orders.Application.Orders.Commands.RefundOrder;

public class RefundOrderValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Refund reason is required")
            .MaximumLength(500)
            .WithMessage("Refund reason cannot exceed 500 characters");
    }
}
