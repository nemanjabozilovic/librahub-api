using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Queries.GetBookInfo;

public record GetBookInfoQuery(Guid BookId) : IRequest<Result<GetBookInfoResponseDto>>;
