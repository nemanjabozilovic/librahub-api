using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Orders.Application.Abstractions;

public interface ILibraryOwnershipClient
{
    Task<Result<bool>> UserOwnsBookAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken = default);

    Task<Result<List<Guid>>> GetOwnedBookIdsAsync(
        Guid userId,
        List<Guid> bookIds,
        CancellationToken cancellationToken = default);
}
