using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Books;
using LibraHub.Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Library.Infrastructure.Projections;

public class BookSnapshotStore : IBookSnapshotStore
{
    private readonly LibraryDbContext _context;

    public BookSnapshotStore(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<BookSnapshot?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.BookSnapshots
            .FirstOrDefaultAsync(s => s.BookId == bookId, cancellationToken);
    }

    public async Task<IReadOnlyList<BookSnapshot>> GetByIdsAsync(IReadOnlyCollection<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        if (bookIds.Count == 0)
        {
            return Array.Empty<BookSnapshot>();
        }

        return await _context.BookSnapshots
            .Where(s => bookIds.Contains(s.BookId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddOrUpdateAsync(BookSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await _context.BookSnapshots
            .FirstOrDefaultAsync(s => s.BookId == snapshot.BookId, cancellationToken);

        if (existing == null)
        {
            await _context.BookSnapshots.AddAsync(snapshot, cancellationToken);
        }
        else
        {
            _context.BookSnapshots.Update(snapshot);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsRemovedAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _context.BookSnapshots
            .FirstOrDefaultAsync(s => s.BookId == bookId, cancellationToken);

        if (snapshot != null)
        {
            snapshot.MarkAsRemoved();
            _context.BookSnapshots.Update(snapshot);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
