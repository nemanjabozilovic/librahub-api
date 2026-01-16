using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using LibraHub.Catalog.Domain.Promotions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Promotions.Queries.GetPricingQuote;

public class GetPricingQuoteHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository,
    IClock clock) : IRequestHandler<GetPricingQuoteQuery, Result<PricingQuoteResponseDto>>
{
    public async Task<Result<PricingQuoteResponseDto>> Handle(GetPricingQuoteQuery request, CancellationToken cancellationToken)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? Currency.USD : request.Currency;

        if (currency != Currency.USD)
        {
            return Result.Failure<PricingQuoteResponseDto>(Error.Validation($"Only {Currency.USD} currency is supported"));
        }

        var utcNow = request.AtUtc?.UtcDateTime ?? clock.UtcNow;

        var items = new List<PricingQuoteItemDto>();

        foreach (var itemRequest in request.Items)
        {
            var book = await bookRepository.GetByIdAsync(itemRequest.BookId, cancellationToken);
            if (book == null)
            {
                return Result.Failure<PricingQuoteResponseDto>(Error.NotFound(CatalogErrors.Book.NotFound));
            }

            var pricing = await pricingRepository.GetByBookIdAsync(itemRequest.BookId, cancellationToken);
            if (pricing == null)
            {
                return Result.Failure<PricingQuoteResponseDto>(Error.NotFound(CatalogErrors.Pricing.NotFound));
            }

            var basePrice = pricing.Price.Amount;
            var bookCurrency = pricing.Price.Currency;

            if (bookCurrency != currency)
            {
                return Result.Failure<PricingQuoteResponseDto>(Error.Validation($"Book {itemRequest.BookId} has price in {bookCurrency}, but only {Currency.USD} is supported"));
            }

            var vatRate = pricing.VatRate ?? 0m;

            var manualPromoActive =
                book.Status == Domain.Books.BookStatus.Published &&
                pricing.PromoPrice != null &&
                !string.IsNullOrWhiteSpace(pricing.PromoName) &&
                pricing.PromoStartDate.HasValue &&
                pricing.PromoEndDate.HasValue &&
                utcNow >= pricing.PromoStartDate.Value &&
                utcNow <= pricing.PromoEndDate.Value;

            var manualPromoNet = manualPromoActive && pricing.PromoPrice != null
                ? pricing.PromoPrice.Amount
                : basePrice;

            var bestNet = manualPromoNet;

            AppliedPromotionDto? appliedPromotion = null;
            if (manualPromoActive && manualPromoNet < basePrice)
            {
                var discountAmount = basePrice - manualPromoNet;

                appliedPromotion = new AppliedPromotionDto
                {
                    Name = pricing.PromoName ?? string.Empty,
                    DiscountType = DiscountType.FixedAmount.ToString(),
                    DiscountValue = discountAmount
                };
            }

            var finalPriceWithVat = ApplyVat(bestNet, vatRate);

            items.Add(new PricingQuoteItemDto
            {
                BookId = itemRequest.BookId,
                BasePrice = basePrice,
                FinalPrice = finalPriceWithVat,
                VatRate = vatRate,
                AppliedPromotion = appliedPromotion
            });
        }

        return Result.Success(new PricingQuoteResponseDto
        {
            Currency = currency,
            Items = items
        });
    }

    private static decimal ApplyVat(decimal net, decimal vatRate)
    {
        if (vatRate <= 0m)
        {
            return net;
        }

        return net * (1m + vatRate / 100m);
    }
}
