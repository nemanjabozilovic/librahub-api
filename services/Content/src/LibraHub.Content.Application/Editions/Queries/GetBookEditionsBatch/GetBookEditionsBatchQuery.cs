using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Editions.Queries.GetBookEditions;
using MediatR;

namespace LibraHub.Content.Application.Editions.Queries.GetBookEditionsBatch;

public record GetBookEditionsBatchQuery(List<Guid> BookIds) : IRequest<Result<Dictionary<Guid, List<BookEditionDto>>>>;
