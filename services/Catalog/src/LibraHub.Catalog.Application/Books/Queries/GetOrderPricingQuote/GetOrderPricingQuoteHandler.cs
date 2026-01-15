using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Errors;
using LibraHub.Catalog.Domain.Promotions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Queries.GetOrderPricingQuote;

public class GetOrderPricingQuoteHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository,
    IPromotionRepository promotionRepository,
    IClock clock) : IRequestHandler<GetOrderPricingQuoteQuery, Result<OrderPricingQuoteResponseDto>>
{
    public async Task<Result<OrderPricingQuoteResponseDto>> Handle(GetOrderPricingQuoteQuery request, CancellationToken cancellationToken)
    {
        if (request.BookIds == null || request.BookIds.Count == 0)
        {
            return Result.Success(new OrderPricingQuoteResponseDto
            {
                Currency = Currency.USD,
                Items = new List<OrderPricingQuoteItemDto>()
            });
        }

        var utcNow = request.AtUtc?.UtcDateTime ?? clock.UtcNow;
        var activeCampaigns = await promotionRepository.GetActiveAsync(utcNow, cancellationToken);
        var evaluator = new PromotionEvaluator();

        var items = new List<OrderPricingQuoteItemDto>();

        foreach (var bookId in request.BookIds.Distinct())
        {
            var book = await bookRepository.GetByIdAsync(bookId, cancellationToken);
            if (book == null)
            {
                return Result.Failure<OrderPricingQuoteResponseDto>(Error.NotFound(CatalogErrors.Book.NotFound));
            }

            var pricing = await pricingRepository.GetByBookIdAsync(bookId, cancellationToken);
            if (pricing == null)
            {
                return Result.Failure<OrderPricingQuoteResponseDto>(Error.NotFound(CatalogErrors.Pricing.NotFound));
            }

            var currency = pricing.Price.Currency;
            if (currency != Currency.USD)
            {
                return Result.Failure<OrderPricingQuoteResponseDto>(Error.Validation($"Only {Currency.USD} currency is supported"));
            }

            var basePrice = pricing.Price.Amount; // net
            var vatRate = pricing.VatRate ?? 0m;

            var isPublished = book.Status == BookStatus.Published;
            var isRemoved = book.Status == BookStatus.Removed;

            // Promo from pricing policy (manual promo window).
            var manualPromoActive =
                isPublished &&
                pricing.PromoPrice != null &&
                !string.IsNullOrWhiteSpace(pricing.PromoName) &&
                pricing.PromoStartDate.HasValue &&
                pricing.PromoEndDate.HasValue &&
                utcNow >= pricing.PromoStartDate.Value &&
                utcNow <= pricing.PromoEndDate.Value;

            var manualPromoNet = manualPromoActive && pricing.PromoPrice != null
                ? pricing.PromoPrice.Amount
                : basePrice;

            // Promo from campaigns/rules.
            var campaignPromo = evaluator.EvaluateBestDiscount(book, basePrice, Currency.USD, utcNow, activeCampaigns);
            var campaignPromoNet = campaignPromo?.FinalPrice ?? basePrice;

            // Pick best net price.
            var finalNet = Math.Min(manualPromoNet, campaignPromoNet);

            Guid? promotionId = null;
            string? promotionName = null;
            decimal? discountAmount = null;

            if (campaignPromo != null && campaignPromoNet <= manualPromoNet && campaignPromoNet < basePrice)
            {
                promotionId = campaignPromo.CampaignId;
                promotionName = campaignPromo.CampaignName;
                discountAmount = basePrice - campaignPromoNet;
            }
            else if (manualPromoActive && manualPromoNet < basePrice)
            {
                promotionId = null;
                promotionName = pricing.PromoName;
                discountAmount = basePrice - manualPromoNet;
            }

            items.Add(new OrderPricingQuoteItemDto
            {
                BookId = book.Id,
                BookTitle = book.Title,
                IsPublished = isPublished,
                IsRemoved = isRemoved,
                BasePrice = basePrice,
                FinalPrice = finalNet,
                VatRate = vatRate,
                PromotionId = promotionId,
                PromotionName = promotionName,
                DiscountAmount = discountAmount
            });
        }

        return Result.Success(new OrderPricingQuoteResponseDto
        {
            Currency = Currency.USD,
            Items = items
        });
    }
}

