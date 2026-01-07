using FluentValidation;

namespace LibraHub.Orders.Application.Orders.Commands.CapturePayment;

public class CapturePaymentValidator : AbstractValidator<CapturePaymentCommand>
{
    public CapturePaymentValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId is required");

        RuleFor(x => x.ProviderReference)
            .NotEmpty()
            .WithMessage("ProviderReference is required");
    }
}
