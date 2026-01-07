using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Content.Infrastructure.Repositories;

public class BookEditionRepository : IBookEditionRepository
{
    private readonly ContentDbContext _context;

    public BookEditionRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<BookEdition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BookEditions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<BookEdition?> GetByBookIdFormatAndVersionAsync(Guid bookId, BookFormat format, int version, CancellationToken cancellationToken = default)
    {
        return await _context.BookEditions
            .FirstOrDefaultAsync(x => x.BookId == bookId && x.Format == format && x.Version == version, cancellationToken);
    }

    public async Task<BookEdition?> GetLatestByBookIdAndFormatAsync(Guid bookId, BookFormat format, CancellationToken cancellationToken = default)
    {
        return await _context.BookEditions
            .Where(x => x.BookId == bookId && x.Format == format)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<BookEdition>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.BookEditions
            .Where(x => x.BookId == bookId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BookEdition>> GetByBookIdsAsync(List<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
        {
            return new List<BookEdition>();
        }

        return await _context.BookEditions
            .Where(x => bookIds.Contains(x.BookId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BookEdition edition, CancellationToken cancellationToken = default)
    {
        await _context.BookEditions.AddAsync(edition, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BookEdition edition, CancellationToken cancellationToken = default)
    {
        _context.BookEditions.Update(edition);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
