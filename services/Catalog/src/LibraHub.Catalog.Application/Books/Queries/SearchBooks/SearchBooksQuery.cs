using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Books.Queries.SearchBooks;

public record SearchBooksQuery(
    string? SearchTerm,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<SearchBooksResponseDto>>;
