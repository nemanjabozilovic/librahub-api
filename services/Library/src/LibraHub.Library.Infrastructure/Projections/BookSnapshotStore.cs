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
