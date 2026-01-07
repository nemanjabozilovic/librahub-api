using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Content.Infrastructure.Repositories;

public class CoverRepository : ICoverRepository
{
    private readonly ContentDbContext _context;

    public CoverRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<Cover?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.Covers
            .FirstOrDefaultAsync(x => x.BookId == bookId, cancellationToken);
    }

    public async Task AddAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        await _context.Covers.AddAsync(cover, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        _context.Covers.Update(cover);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Cover cover, CancellationToken cancellationToken = default)
    {
        _context.Covers.Remove(cover);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
