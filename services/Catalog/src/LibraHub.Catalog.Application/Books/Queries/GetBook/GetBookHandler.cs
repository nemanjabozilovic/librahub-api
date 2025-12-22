using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Queries.GetBook;

public class GetBookHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository) : IRequestHandler<GetBookQuery, Result<GetBookResponseDto>>
{
    public async Task<Result<GetBookResponseDto>> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null)
        {
            return Result.Failure<GetBookResponseDto>(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        var pricing = await pricingRepository.GetByBookIdAsync(request.BookId, cancellationToken);

        var response = new GetBookResponseDto
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            Language = book.Language,
            Publisher = book.Publisher,
            PublicationDate = book.PublicationDate,
            Isbn = book.Isbn?.Value,
            Status = book.Status.ToString(),
            Authors = book.Authors.Select(a => a.Name).ToList(),
            Categories = book.Categories.Select(c => c.Name).ToList(),
            Tags = book.Tags.Select(t => t.Name).ToList(),
            Pricing = pricing != null ? new PricingDto
            {
                Price = pricing.Price.Amount,
                Currency = pricing.Price.Currency,
                VatRate = pricing.VatRate,
                PromoPrice = pricing.PromoPrice?.Amount,
                PromoStartDate = pricing.PromoStartDate,
                PromoEndDate = pricing.PromoEndDate
            } : null
        };

        return Result.Success(response);
    }
}
