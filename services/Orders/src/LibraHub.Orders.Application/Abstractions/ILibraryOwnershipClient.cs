namespace LibraHub.Orders.Application.Abstractions;

public interface ILibraryOwnershipClient
{
    Task<bool> UserOwnsBookAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> GetOwnedBookIdsAsync(
        Guid userId,
        List<Guid> bookIds,
        CancellationToken cancellationToken = default);
}
