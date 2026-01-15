using LibraHub.Catalog.Domain.Books;

namespace LibraHub.Catalog.Domain.Promotions;

public class PromotionEvaluator
{
    public PromotionResult? EvaluateBestDiscount(
        Book book,
        decimal basePrice,
        string currency,
        DateTime utcNow,
        List<PromotionCampaign> activeCampaigns)
    {
        if (book.Status != BookStatus.Published)
        {
            return null;
        }

        if (basePrice <= 0)
        {
            return null;
        }

        var applicableRules = new List<(PromotionRule Rule, PromotionCampaign Campaign)>();

        foreach (var campaign in activeCampaigns.Where(c => c.IsActive(utcNow)))
        {
            foreach (var rule in campaign.Rules)
            {
                if (IsBookEligible(book, rule))
                {
                    applicableRules.Add((rule, campaign));
                }
            }
        }

        if (!applicableRules.Any())
        {
            return null;
        }

        PromotionResult? bestResult = null;
        decimal bestFinalPrice = basePrice;

        foreach (var (rule, campaign) in applicableRules)
        {
            var finalPrice = rule.CalculateFinalPrice(basePrice, currency);
            var discount = basePrice - finalPrice;

            if (finalPrice < bestFinalPrice)
            {
                bestFinalPrice = finalPrice;
                bestResult = new PromotionResult
                {
                    CampaignId = campaign.Id,
                    RuleId = rule.Id,
                    CampaignName = campaign.Name,
                    DiscountType = rule.DiscountType,
                    DiscountValue = rule.DiscountValue,
                    FinalPrice = finalPrice,
                    DiscountAmount = discount
                };
            }
        }

        return bestResult;
    }

    private bool IsBookEligible(Book book, PromotionRule rule)
    {
        if (rule.Exclusions != null && rule.Exclusions.Contains(book.Id))
        {
            return false;
        }

        return rule.AppliesToScope switch
        {
            PromotionScope.All => true,
            PromotionScope.BookSet => rule.ScopeValues != null && rule.ScopeValues.Contains(book.Id.ToString()),
            PromotionScope.Author => rule.ScopeValues != null && book.Authors.Any(a => rule.ScopeValues.Contains(a.Name)),
            PromotionScope.Category => rule.ScopeValues != null && book.Categories.Any(c => rule.ScopeValues.Contains(c.Name)),
            PromotionScope.Tag => rule.ScopeValues != null && book.Tags.Any(t => rule.ScopeValues.Contains(t.Name)),
            _ => false
        };
    }
}

public class PromotionResult
{
    public Guid CampaignId { get; init; }
    public Guid RuleId { get; init; }
    public string CampaignName { get; init; } = string.Empty;
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal DiscountAmount { get; init; }
}
