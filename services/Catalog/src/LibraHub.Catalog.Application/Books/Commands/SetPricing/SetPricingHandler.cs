using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Commands.SetPricing;

public class SetPricingHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository,
    IOutboxWriter outboxWriter) : IRequestHandler<SetPricingCommand, Result>
{
    public async Task<Result> Handle(SetPricingCommand request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        var money = new Money(request.Price, request.Currency);
        var existingPricing = await pricingRepository.GetByBookIdAsync(request.BookId, cancellationToken);

        if (existingPricing == null)
        {
            existingPricing = new PricingPolicy(Guid.NewGuid(), request.BookId, money, request.VatRate);
            await pricingRepository.AddAsync(existingPricing, cancellationToken);
        }
        else
        {
            existingPricing.UpdatePrice(money, request.VatRate);
            await pricingRepository.UpdateAsync(existingPricing, cancellationToken);
        }

        if (request.PromoPrice.HasValue && request.PromoStartDate.HasValue && request.PromoEndDate.HasValue)
        {
            var promoMoney = new Money(request.PromoPrice.Value, request.Currency);
            existingPricing.SetPromo(promoMoney, request.PromoStartDate.Value, request.PromoEndDate.Value);
            await pricingRepository.UpdateAsync(existingPricing, cancellationToken);
        }
        else if (existingPricing.PromoPrice != null)
        {
            existingPricing.ClearPromo();
            await pricingRepository.UpdateAsync(existingPricing, cancellationToken);
        }

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookPricingChangedV1
            {
                BookId = book.Id,
                Price = existingPricing.Price.Amount,
                Currency = existingPricing.Price.Currency,
                VatRate = existingPricing.VatRate,
                UpdatedAt = existingPricing.UpdatedAt
            },
            Contracts.Common.EventTypes.BookPricingChanged,
            cancellationToken);

        return Result.Success();
    }
}
