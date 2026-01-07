using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using MediatR;

namespace LibraHub.Content.Application.Editions.Queries.GetBookEditionsBatch;

public class GetBookEditionsBatchHandler(
    IBookEditionRepository editionRepository) : IRequestHandler<GetBookEditionsBatchQuery, Result<Dictionary<Guid, List<GetBookEditions.BookEditionDto>>>>
{
    public async Task<Result<Dictionary<Guid, List<GetBookEditions.BookEditionDto>>>> Handle(GetBookEditionsBatchQuery request, CancellationToken cancellationToken)
    {
        if (request.BookIds == null || request.BookIds.Count == 0)
        {
            return Result.Success(new Dictionary<Guid, List<GetBookEditions.BookEditionDto>>());
        }

        var allEditions = await editionRepository.GetByBookIdsAsync(request.BookIds, cancellationToken);

        var result = allEditions
            .Where(e => e.IsAccessible)
            .GroupBy(e => e.BookId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => new GetBookEditions.BookEditionDto
                {
                    Id = e.Id,
                    Format = e.Format.ToString().ToUpperInvariant(),
                    Version = e.Version,
                    UploadedAt = new DateTimeOffset(e.UploadedAt, TimeSpan.Zero)
                })
                .OrderByDescending(e => e.Format)
                .ThenByDescending(e => e.Version)
                .ToList());

        foreach (var bookId in request.BookIds)
        {
            if (!result.ContainsKey(bookId))
            {
                result[bookId] = new List<GetBookEditions.BookEditionDto>();
            }
        }

        return Result.Success(result);
    }
}
