using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Queries.GetBookInfo;

public class GetBookInfoHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository) : IRequestHandler<GetBookInfoQuery, Result<GetBookInfoResponseDto>>
{
    public async Task<Result<GetBookInfoResponseDto>> Handle(GetBookInfoQuery request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure<GetBookInfoResponseDto>(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        var pricing = await pricingRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        var isFree = pricing?.Price.IsFree ?? false;
        var isBlocked = book.Status == BookStatus.Removed;

        var response = new GetBookInfoResponseDto
        {
            BookId = book.Id,
            IsFree = isFree,
            IsBlocked = isBlocked
        };

        return Result.Success(response);
    }
}
