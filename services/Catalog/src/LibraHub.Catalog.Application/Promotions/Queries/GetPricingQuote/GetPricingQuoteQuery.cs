using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Promotions.Queries.GetPricingQuote;

public record PricingQuoteItemRequest
{
    public Guid BookId { get; init; }
}

public record GetPricingQuoteQuery(
    string Currency,
    List<PricingQuoteItemRequest> Items,
    DateTimeOffset? AtUtc = null) : IRequest<Result<PricingQuoteResponseDto>>;
