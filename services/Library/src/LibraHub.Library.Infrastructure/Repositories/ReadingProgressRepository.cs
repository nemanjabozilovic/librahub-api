using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Reading;
using LibraHub.Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Library.Infrastructure.Repositories;

public class ReadingProgressRepository : IReadingProgressRepository
{
    private readonly LibraryDbContext _context;

    public ReadingProgressRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<ReadingProgress?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.ReadingProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.BookId == bookId, cancellationToken);
    }

    public async Task<ReadingProgress?> GetByUserBookFormatAndVersionAsync(
        Guid userId,
        Guid bookId,
        string? format,
        int? version,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReadingProgress
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.BookId == bookId &&
                p.Format == format &&
                p.Version == version,
                cancellationToken);
    }

    public async Task AddAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        await _context.ReadingProgress.AddAsync(progress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ReadingProgress>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.ReadingProgress
            .Where(p => p.BookId == bookId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReadingProgress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ReadingProgress
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        _context.ReadingProgress.Update(progress);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ReadingProgress progress, CancellationToken cancellationToken = default)
    {
        _context.ReadingProgress.Remove(progress);

        if (_context.Database.CurrentTransaction == null)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
