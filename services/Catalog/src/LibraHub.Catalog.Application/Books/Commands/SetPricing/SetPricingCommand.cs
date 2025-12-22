using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Commands.SetPricing;

public record SetPricingCommand(
    Guid BookId,
    decimal Price,
    string Currency,
    decimal? VatRate,
    decimal? PromoPrice,
    DateTime? PromoStartDate,
    DateTime? PromoEndDate) : IRequest<Result>;
