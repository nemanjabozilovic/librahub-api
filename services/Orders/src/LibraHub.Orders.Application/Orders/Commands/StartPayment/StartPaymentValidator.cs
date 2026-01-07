using FluentValidation;

namespace LibraHub.Orders.Application.Orders.Commands.StartPayment;

public class StartPaymentValidator : AbstractValidator<StartPaymentCommand>
{
    public StartPaymentValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required")
            .Must(p => Enum.TryParse<LibraHub.Orders.Domain.Payments.PaymentProvider>(p, ignoreCase: true, out _))
            .WithMessage("Invalid payment provider");
    }
}
