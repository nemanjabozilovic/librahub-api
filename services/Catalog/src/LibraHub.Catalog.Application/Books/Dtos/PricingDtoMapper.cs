using LibraHub.Catalog.Domain.Books;

namespace LibraHub.Catalog.Application.Books.Dtos;

public static class PricingDtoMapper
{
    public static PricingDto? MapFromPricingPolicy(PricingPolicy? pricing)
    {
        if (pricing == null)
        {
            return null;
        }

        return new PricingDto
        {
            Price = pricing.Price.Amount,
            Currency = pricing.Price.Currency,
            VatRate = pricing.VatRate,
            PriceWithVat = CalculatePriceWithVat(pricing.Price.Amount, pricing.VatRate),
            PromoPrice = pricing.PromoPrice?.Amount,
            PromoPriceWithVat = pricing.PromoPrice != null
                ? CalculatePriceWithVat(pricing.PromoPrice.Amount, pricing.VatRate)
                : null,
            PromoStartDate = pricing.PromoStartDate.HasValue
                ? new DateTimeOffset(pricing.PromoStartDate.Value, TimeSpan.Zero)
                : null,
            PromoEndDate = pricing.PromoEndDate.HasValue
                ? new DateTimeOffset(pricing.PromoEndDate.Value, TimeSpan.Zero)
                : null
        };
    }

    private static decimal CalculatePriceWithVat(decimal netPrice, decimal? vatRate)
    {
        if (!vatRate.HasValue || vatRate.Value <= 0m)
        {
            return netPrice;
        }

        return netPrice * (1 + vatRate.Value / 100m);
    }
}
