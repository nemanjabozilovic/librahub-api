using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Library.Application.Abstractions;

public interface ICatalogClient
{
    Task<Result<CatalogBookDetailsDto>> GetBookDetailsAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Result<Dictionary<Guid, CatalogBookDetailsDto>>> GetBookDetailsByIdsAsync(
        List<Guid> bookIds,
        CancellationToken cancellationToken = default);
}

public record CatalogBookDetailsDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> Authors { get; init; } = new();
    public List<string> Categories { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public string? CoverUrl { get; init; }
    public bool HasEdition { get; init; }
    public List<CatalogEditionDto> Editions { get; init; } = new();
}

public record CatalogEditionDto
{
    public Guid Id { get; init; }
    public string Format { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
