using FluentValidation;
using LibraHub.BuildingBlocks.Constants;

namespace LibraHub.Catalog.Application.Promotions.Commands.AddPromotionRule;

public class AddPromotionRuleValidator : AbstractValidator<AddPromotionRuleCommand>
{
    public AddPromotionRuleValidator()
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty().WithMessage("Campaign ID is required");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than zero");

        RuleFor(x => x.Currency)
            .Equal(Currency.USD)
            .When(x => x.DiscountType == Domain.Promotions.DiscountType.FixedAmount)
            .WithMessage($"Only {Currency.USD} currency is supported for fixed amount discount");
    }
}
