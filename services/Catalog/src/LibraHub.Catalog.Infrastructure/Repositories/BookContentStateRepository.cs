using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Projections;
using LibraHub.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Catalog.Infrastructure.Repositories;

public class BookContentStateRepository : IBookContentStateRepository
{
    private readonly CatalogDbContext _context;

    public BookContentStateRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<BookContentState?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.BookContentStates
            .FirstOrDefaultAsync(s => s.BookId == bookId, cancellationToken);
    }

    public async Task<Dictionary<Guid, BookContentState>> GetByBookIdsAsync(IEnumerable<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        var bookIdsList = bookIds.ToList();
        if (bookIdsList.Count == 0)
        {
            return new Dictionary<Guid, BookContentState>();
        }

        var states = await _context.BookContentStates
            .Where(s => bookIdsList.Contains(s.BookId))
            .ToListAsync(cancellationToken);

        return states.ToDictionary(s => s.BookId);
    }

    public async Task AddAsync(BookContentState state, CancellationToken cancellationToken = default)
    {
        await _context.BookContentStates.AddAsync(state, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BookContentState state, CancellationToken cancellationToken = default)
    {
        _context.BookContentStates.Update(state);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
