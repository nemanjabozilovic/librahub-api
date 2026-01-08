using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Content.Application.Covers.Queries.GetBookCover;

public record GetBookCoverQuery(Guid BookId) : IRequest<Result<BookCoverDto>>;

