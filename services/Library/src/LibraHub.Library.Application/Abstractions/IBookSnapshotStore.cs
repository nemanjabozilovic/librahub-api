using LibraHub.Library.Domain.Books;

namespace LibraHub.Library.Application.Abstractions;

public interface IBookSnapshotStore
{
    Task<BookSnapshot?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddOrUpdateAsync(BookSnapshot snapshot, CancellationToken cancellationToken = default);

    Task MarkAsRemovedAsync(Guid bookId, CancellationToken cancellationToken = default);
}
