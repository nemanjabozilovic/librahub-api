using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Books.Dtos;
using LibraHub.Catalog.Application.Options;
using MediatR;
using Microsoft.Extensions.Options;

namespace LibraHub.Catalog.Application.Books.Queries.SearchBooks;

public class SearchBooksHandler(
    IBookRepository bookRepository,
    IPricingRepository pricingRepository,
    IBookContentStateRepository contentStateRepository,
    IContentReadClient contentReadClient,
    ICurrentUser currentUser,
    IOptions<CatalogOptions> options,
    ICache cache) : IRequestHandler<SearchBooksQuery, Result<SearchBooksResponseDto>>
{
    public async Task<Result<SearchBooksResponseDto>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        var includeAllStatuses = currentUser.IsInRole("Librarian") || currentUser.IsInRole("Admin");
        var cacheKey = CacheKeys.GetSearchBooksKey(request.SearchTerm, request.Page, request.PageSize, includeAllStatuses);

        var cachedResult = await cache.GetAsync<SearchBooksResponseDto>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return Result.Success(cachedResult);
        }

        var books = await bookRepository.SearchAsync(request.SearchTerm, request.Page, request.PageSize, includeAllStatuses, cancellationToken);
        var totalCount = await bookRepository.CountSearchAsync(request.SearchTerm, includeAllStatuses, cancellationToken);

        var bookIds = books.Select(b => b.Id).ToList();

        var pricingDict = await pricingRepository.GetByBookIdsAsync(bookIds, cancellationToken);
        var contentStateDict = await contentStateRepository.GetByBookIdsAsync(bookIds, cancellationToken);

        var editionsDictResult = await contentReadClient.GetBookEditionsBatchAsync(bookIds, cancellationToken);
        var editionsDict = editionsDictResult.IsSuccess
            ? editionsDictResult.Value
            : bookIds.ToDictionary(id => id, _ => new List<Abstractions.BookEditionInfoDto>());

        var bookSummaries = books.Select(b =>
        {
            var pricing = pricingDict.GetValueOrDefault(b.Id);
            var contentState = contentStateDict.GetValueOrDefault(b.Id);
            var editions = editionsDict.GetValueOrDefault(b.Id) ?? new List<Abstractions.BookEditionInfoDto>();
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

            return new BookSummaryDto
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                Language = b.Language,
                Publisher = b.Publisher,
                PublicationDate = b.PublicationDate.HasValue ? new DateTimeOffset(b.PublicationDate.Value, TimeSpan.Zero) : null,
                Isbn = b.Isbn?.Value,
                Status = b.Status.ToString(),
                Authors = b.Authors.Select(a => a.Name).ToList(),
                Categories = b.Categories.Select(c => c.Name).ToList(),
                Tags = b.Tags.Select(t => t.Name).ToList(),
                Pricing = PricingDtoMapper.MapFromPricingPolicy(pricing),
                CoverUrl = !string.IsNullOrWhiteSpace(coverRef)
                    ? $"{options.Value.GatewayBaseUrl}/api/covers/{coverRef}"
                    : null,
                HasEdition = hasEdition,
                Editions = editionDtos
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var response = new SearchBooksResponseDto
        {
            Books = bookSummaries,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };

        await cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(1), cancellationToken);

        return Result.Success(response);
    }
}
