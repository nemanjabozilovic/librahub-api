using FluentValidation;
using LibraHub.BuildingBlocks.Constants;

namespace LibraHub.Catalog.Application.Books.Commands.SetPricing;

public class SetPricingValidator : AbstractValidator<SetPricingCommand>
{
    public SetPricingValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty().WithMessage("Book ID is required");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Equal(Currency.USD).WithMessage($"Only {Currency.USD} currency is supported");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100).WithMessage("VAT rate must be between 0 and 100")
            .When(x => x.VatRate.HasValue);

        RuleFor(x => x)
            .Must(x => !x.PromoPrice.HasValue || (x.PromoStartDate.HasValue && x.PromoEndDate.HasValue))
            .WithMessage("Promo dates are required when promo price is set")
            .Must(x => !x.PromoStartDate.HasValue || x.PromoStartDate < x.PromoEndDate)
            .WithMessage("Promo start date must be before end date")
            .When(x => x.PromoPrice.HasValue);
    }
}
