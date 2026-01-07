using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Content.Application.Editions.Queries.GetBookEditions;

public record GetBookEditionsQuery(Guid BookId) : IRequest<Result<List<BookEditionDto>>>;
