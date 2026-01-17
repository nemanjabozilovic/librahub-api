using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Dtos;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Books.Queries.GetBook;

public class GetBookHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository,
    IBookContentStateRepository contentStateRepository,
    IContentReadClient contentReadClient,
    IOptions<CatalogOptions> options,
    ICache cache) : IRequestHandler<GetBookQuery, Result<GetBookResponseDto>>
{
    public async Task<Result<GetBookResponseDto>> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.GetBookKey(request.BookId);
        var cachedResult = await cache.GetAsync<GetBookResponseDto>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return Result.Success(cachedResult);
        }

        var book = await bookRepository.GetByIdAsync(request.BookId, cancellationToken);
        if (book == null || book.Status == Domain.Books.BookStatus.Removed)
        {
            return Result.Failure<GetBookResponseDto>(Error.NotFound(CatalogErrors.Book.NotFound));
        }

        var pricing = await pricingRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        var contentState = await contentStateRepository.GetByBookIdAsync(request.BookId, cancellationToken);

        var editionsResult = await contentReadClient.GetBookEditionsAsync(request.BookId, cancellationToken);
        var editions = editionsResult.IsSuccess ? editionsResult.Value : new List<Abstractions.BookEditionInfoDto>();

        var editionDtos = editions.Select(e => new Dtos.EditionDto
        {
            Id = e.Id,
            Format = e.Format,
            Version = e.Version,
            UploadedAt = e.UploadedAt
        }).ToList();

        var coverRef = contentState?.CoverRef;

        var hasEdition = contentState?.HasEdition ?? false;
        if (!hasEdition && editionDtos.Count > 0)
        {
            hasEdition = true;
        }

        var response = new GetBookResponseDto
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            Language = book.Language,
            Publisher = book.Publisher,
            PublicationDate = book.PublicationDate.HasValue ? new DateTimeOffset(book.PublicationDate.Value, TimeSpan.Zero) : null,
            Isbn = book.Isbn?.Value,
            Status = book.Status.ToString(),
            Authors = book.Authors.Select(a => a.Name).ToList(),
            Categories = book.Categories.Select(c => c.Name).ToList(),
            Tags = book.Tags.Select(t => t.Name).ToList(),
            Pricing = PricingDtoMapper.MapFromPricingPolicy(pricing),
            CoverUrl = !string.IsNullOrWhiteSpace(coverRef)
                ? $"{options.Value.GatewayBaseUrl}/api/covers/{coverRef}"
                : null,
            HasEdition = hasEdition,
            Editions = editionDtos
        };

        await cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

        return Result.Success(response);
    }
}
