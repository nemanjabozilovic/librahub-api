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
    IPromotionRepository promotionRepository,
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
        var activeCampaigns = await promotionRepository.GetActiveAsync(utcNow, cancellationToken);
        var evaluator = new PromotionEvaluator();

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

            var promotionResult = evaluator.EvaluateBestDiscount(book, basePrice, currency, utcNow, activeCampaigns);

            AppliedPromotionDto? appliedPromotion = null;
            if (promotionResult != null)
            {
                appliedPromotion = new AppliedPromotionDto
                {
                    CampaignId = promotionResult.CampaignId,
                    RuleId = promotionResult.RuleId,
                    Name = promotionResult.CampaignName,
                    DiscountType = promotionResult.DiscountType.ToString(),
                    DiscountValue = promotionResult.DiscountValue
                };
            }

            items.Add(new PricingQuoteItemDto
            {
                BookId = itemRequest.BookId,
                BasePrice = basePrice,
                FinalPrice = promotionResult?.FinalPrice ?? basePrice,
                VatRate = pricing.VatRate ?? 0m,
                AppliedPromotion = appliedPromotion
            });
        }

        return Result.Success(new PricingQuoteResponseDto
        {
            Currency = currency,
            Items = items
        });
    }
}
