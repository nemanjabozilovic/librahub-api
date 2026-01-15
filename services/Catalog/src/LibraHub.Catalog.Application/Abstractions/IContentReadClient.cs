using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Catalog.Application.Abstractions;

public interface IContentReadClient
{
    Task<Result<string?>> GetBookCoverRefAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Result<List<BookEditionInfoDto>>> GetBookEditionsAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Result<Dictionary<Guid, List<BookEditionInfoDto>>>> GetBookEditionsBatchAsync(List<Guid> bookIds, CancellationToken cancellationToken = default);
}

public record BookEditionInfoDto
{
    public Guid Id { get; init; }
    public string Format { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
