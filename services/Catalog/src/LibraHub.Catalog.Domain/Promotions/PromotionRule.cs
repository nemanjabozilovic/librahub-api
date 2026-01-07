namespace LibraHub.Catalog.Domain.Promotions;

public class PromotionRule
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public string? Currency { get; private set; }
    public decimal? MinPriceAfterDiscount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public PromotionScope AppliesToScope { get; private set; }
    public List<string>? ScopeValues { get; private set; }
    public List<Guid>? Exclusions { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private const string SupportedCurrency = "USD";

    protected PromotionRule()
    { } // For EF Core

    public PromotionRule(
        Guid id,
        Guid campaignId,
        DiscountType discountType,
        decimal discountValue,
        string? currency,
        decimal? minPriceAfterDiscount,
        decimal? maxDiscountAmount,
        PromotionScope appliesToScope,
        List<string>? scopeValues,
        List<Guid>? exclusions)
    {
        Id = id;
        CampaignId = campaignId;
        DiscountType = discountType;
        DiscountValue = discountValue;
        Currency = currency;
        MinPriceAfterDiscount = minPriceAfterDiscount;
        MaxDiscountAmount = maxDiscountAmount;
        AppliesToScope = appliesToScope;
        ScopeValues = scopeValues;
        Exclusions = exclusions;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    private void Validate()
    {
        if (DiscountValue <= 0)
        {
            throw new ArgumentException("Discount value must be greater than zero", nameof(DiscountValue));
        }

        if (DiscountType == DiscountType.Percentage && DiscountValue > 100)
        {
            throw new ArgumentException("Percentage discount cannot exceed 100%", nameof(DiscountValue));
        }

        if (DiscountType == DiscountType.FixedAmount)
        {
            if (string.IsNullOrWhiteSpace(Currency))
            {
                throw new ArgumentException("Currency is required for fixed amount discount", nameof(Currency));
            }

            if (Currency != SupportedCurrency)
            {
                throw new ArgumentException($"Only {SupportedCurrency} currency is supported", nameof(Currency));
            }
        }

        if (MinPriceAfterDiscount.HasValue && MinPriceAfterDiscount.Value < 0)
        {
            throw new ArgumentException("Minimum price after discount cannot be negative", nameof(MinPriceAfterDiscount));
        }

        if (MaxDiscountAmount.HasValue && MaxDiscountAmount.Value < 0)
        {
            throw new ArgumentException("Maximum discount amount cannot be negative", nameof(MaxDiscountAmount));
        }
    }

    public decimal CalculateDiscount(decimal basePrice, string currency)
    {
        decimal discount = 0;

        if (DiscountType == DiscountType.Percentage)
        {
            discount = basePrice * (DiscountValue / 100m);
        }
        else if (DiscountType == DiscountType.FixedAmount)
        {
            if (Currency != currency)
            {
                return 0; // Currency mismatch, no discount
            }
            discount = DiscountValue;
        }

        // Apply max discount cap if specified
        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
        {
            discount = MaxDiscountAmount.Value;
        }

        return discount;
    }

    public decimal CalculateFinalPrice(decimal basePrice, string currency)
    {
        var discount = CalculateDiscount(basePrice, currency);
        var finalPrice = basePrice - discount;

        // Ensure minimum price
        if (MinPriceAfterDiscount.HasValue && finalPrice < MinPriceAfterDiscount.Value)
        {
            finalPrice = MinPriceAfterDiscount.Value;
        }

        // Ensure non-negative
        if (finalPrice < 0)
        {
            finalPrice = 0;
        }

        return Math.Round(finalPrice, 2, MidpointRounding.AwayFromZero);
    }
}
