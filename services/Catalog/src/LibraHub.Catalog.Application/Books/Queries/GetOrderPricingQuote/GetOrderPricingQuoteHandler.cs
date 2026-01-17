using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Constants;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Queries.GetOrderPricingQuote;

public class GetOrderPricingQuoteHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository,
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

            var basePrice = pricing.Price.Amount;
            var vatRate = pricing.VatRate ?? 0m;

            var isPublished = book.Status == BookStatus.Published;
            var isRemoved = book.Status == BookStatus.Removed;
            var manualPromoActive =
                isPublished &&
                pricing.PromoPrice != null &&
                !string.IsNullOrWhiteSpace(pricing.PromoName) &&
                pricing.PromoStartDate.HasValue &&
                pricing.PromoEndDate.HasValue &&
                utcNow >= pricing.PromoStartDate.Value &&
                utcNow <= pricing.PromoEndDate.Value;

            var finalNet = manualPromoActive && pricing.PromoPrice != null
                ? pricing.PromoPrice.Amount
                : basePrice;

            Guid? promotionId = null;
            string? promotionName = null;
            decimal? discountAmount = null;

            if (manualPromoActive && finalNet < basePrice)
            {
                promotionId = null;
                promotionName = pricing.PromoName;
                discountAmount = basePrice - finalNet;
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
