using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Content.Application.Abstractions;

public interface ICatalogReadClient
{
    Task<Result<BookInfo>> GetBookInfoAsync(Guid bookId, CancellationToken cancellationToken = default);
}

public record BookInfo
{
    public Guid BookId { get; init; }
    public bool IsFree { get; init; }
    public bool IsBlocked { get; init; }
}
