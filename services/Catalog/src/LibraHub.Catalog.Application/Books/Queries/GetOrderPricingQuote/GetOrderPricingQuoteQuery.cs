using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Queries.GetOrderPricingQuote;

public record GetOrderPricingQuoteQuery(
    List<Guid> BookIds,
    Guid? UserId = null,
    DateTimeOffset? AtUtc = null) : IRequest<Result<OrderPricingQuoteResponseDto>>;

