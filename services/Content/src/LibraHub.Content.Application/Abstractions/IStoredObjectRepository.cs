using LibraHub.Content.Domain.Storage;

namespace LibraHub.Content.Application.Abstractions;

public interface IStoredObjectRepository
{
    Task<StoredObject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StoredObject?> GetByBookIdAndKeyAsync(Guid bookId, string objectKey, CancellationToken cancellationToken = default);

    Task<List<StoredObject>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddAsync(StoredObject storedObject, CancellationToken cancellationToken = default);

    Task UpdateAsync(StoredObject storedObject, CancellationToken cancellationToken = default);

    Task DeleteAsync(StoredObject storedObject, CancellationToken cancellationToken = default);
}
