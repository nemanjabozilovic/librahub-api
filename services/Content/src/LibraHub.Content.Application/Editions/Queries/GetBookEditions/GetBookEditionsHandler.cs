using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using MediatR;

namespace LibraHub.Content.Application.Editions.Queries.GetBookEditions;

public class GetBookEditionsHandler(
    IBookEditionRepository editionRepository) : IRequestHandler<GetBookEditionsQuery, Result<List<BookEditionDto>>>
{
    public async Task<Result<List<BookEditionDto>>> Handle(GetBookEditionsQuery request, CancellationToken cancellationToken)
    {
        var editions = await editionRepository.GetByBookIdAsync(request.BookId, cancellationToken);

        var accessibleEditions = editions
            .Where(e => e.IsAccessible)
            .OrderByDescending(e => e.Format)
            .ThenByDescending(e => e.Version)
            .ToList();

        var result = accessibleEditions.Select(e => new BookEditionDto
        {
            Id = e.Id,
            Format = e.Format.ToString().ToUpperInvariant(),
            Version = e.Version,
            UploadedAt = new DateTimeOffset(e.UploadedAt, TimeSpan.Zero)
        }).ToList();

        return Result.Success(result);
    }
}
