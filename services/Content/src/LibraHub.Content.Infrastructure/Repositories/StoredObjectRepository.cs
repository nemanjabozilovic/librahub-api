using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Domain.Storage;
using LibraHub.Content.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Content.Infrastructure.Repositories;

public class StoredObjectRepository : IStoredObjectRepository
{
    private readonly ContentDbContext _context;

    public StoredObjectRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<StoredObject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.StoredObjects
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<StoredObject?> GetByBookIdAndKeyAsync(Guid bookId, string objectKey, CancellationToken cancellationToken = default)
    {
        return await _context.StoredObjects
            .FirstOrDefaultAsync(x => x.BookId == bookId && x.ObjectKey == objectKey, cancellationToken);
    }

    public async Task<List<StoredObject>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.StoredObjects
            .Where(x => x.BookId == bookId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StoredObject storedObject, CancellationToken cancellationToken = default)
    {
        await _context.StoredObjects.AddAsync(storedObject, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StoredObject storedObject, CancellationToken cancellationToken = default)
    {
        _context.StoredObjects.Update(storedObject);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(StoredObject storedObject, CancellationToken cancellationToken = default)
    {
        _context.StoredObjects.Remove(storedObject);

        if (_context.Database.CurrentTransaction == null)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
