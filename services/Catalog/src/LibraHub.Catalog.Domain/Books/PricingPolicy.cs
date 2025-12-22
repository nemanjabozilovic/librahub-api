namespace LibraHub.Catalog.Domain.Books;

public class PricingPolicy
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Money Price { get; private set; } = null!;
    public decimal? VatRate { get; private set; }
    public DateTime? PromoStartDate { get; private set; }
    public DateTime? PromoEndDate { get; private set; }
    public Money? PromoPrice { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PricingPolicy()
    { } // For EF Core

    public PricingPolicy(Guid id, Guid bookId, Money price, decimal? vatRate = null)
    {
        Id = id;
        BookId = bookId;
        Price = price;
        VatRate = vatRate;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(Money newPrice, decimal? vatRate = null)
    {
        Price = newPrice;
        VatRate = vatRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPromo(Money promoPrice, DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
        {
            throw new ArgumentException("Promo start date must be before end date");
        }

        PromoPrice = promoPrice;
        PromoStartDate = startDate;
        PromoEndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearPromo()
    {
        PromoPrice = null;
        PromoStartDate = null;
        PromoEndDate = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public Money GetCurrentPrice(DateTime utcNow)
    {
        if (PromoPrice != null && PromoStartDate.HasValue && PromoEndDate.HasValue)
        {
            if (utcNow >= PromoStartDate.Value && utcNow <= PromoEndDate.Value)
            {
                return PromoPrice;
            }
        }

        return Price;
    }

    public bool IsValid()
    {
        if (Price.IsFree)
        {
            return true;
        }

        return Price.Amount > 0;
    }
}
