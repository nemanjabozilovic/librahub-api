using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Queries.GetBook;

public record GetBookQuery(Guid BookId) : IRequest<Result<GetBookResponseDto>>;
