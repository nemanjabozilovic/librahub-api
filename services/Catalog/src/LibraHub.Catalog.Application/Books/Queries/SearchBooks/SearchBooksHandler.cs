using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Queries.SearchBooks;

public class SearchBooksHandler(
    IBookRepository bookRepository) : IRequestHandler<SearchBooksQuery, Result<SearchBooksResponseDto>>
{
    public async Task<Result<SearchBooksResponseDto>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        var booksTask = bookRepository.SearchAsync(request.SearchTerm, request.Page, request.PageSize, cancellationToken);
        var totalCountTask = bookRepository.CountSearchAsync(request.SearchTerm, cancellationToken);

        await Task.WhenAll(booksTask, totalCountTask);

        var books = await booksTask;
        var totalCount = await totalCountTask;

        var bookSummaries = books.Select(b => new BookSummaryDto
        {
            Id = b.Id,
            Title = b.Title,
            Description = b.Description,
            Status = b.Status.ToString(),
            Authors = b.Authors.Select(a => a.Name).ToList()
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

        return Result.Success(response);
    }
}
