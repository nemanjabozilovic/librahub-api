using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using MediatR;

namespace LibraHub.Content.Application.Covers.Queries.GetBookCover;

public class GetBookCoverHandler(
    ICoverRepository coverRepository,
    IStoredObjectRepository storedObjectRepository) : IRequestHandler<GetBookCoverQuery, Result<BookCoverDto>>
{
    public async Task<Result<BookCoverDto>> Handle(GetBookCoverQuery request, CancellationToken cancellationToken)
    {
        var cover = await coverRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        if (cover == null || !cover.IsAccessible)
        {
            return Result.Success(new BookCoverDto { CoverRef = null });
        }

        var storedObject = await storedObjectRepository.GetByIdAsync(cover.StoredObjectId, cancellationToken);
        if (storedObject == null)
        {
            return Result.Success(new BookCoverDto { CoverRef = null });
        }

        return Result.Success(new BookCoverDto { CoverRef = storedObject.ObjectKey });
    }
}
