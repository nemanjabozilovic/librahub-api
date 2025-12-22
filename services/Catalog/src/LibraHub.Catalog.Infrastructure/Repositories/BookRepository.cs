using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Catalog.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly CatalogDbContext _context;

    public BookRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.Tags)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<List<Book>> SearchAsync(string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(searchTerm);

        return await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSearchAsync(string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(searchTerm);
        return await query.CountAsync(cancellationToken);
    }

    private IQueryable<Book> BuildSearchQuery(string? searchTerm)
    {
        var query = _context.Books
            .Include(b => b.Authors)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(b =>
                b.Title.ToLower().Contains(term) ||
                (b.Description != null && b.Description.ToLower().Contains(term)) ||
                b.Authors.Any(a => a.Name.ToLower().Contains(term)));
        }

        // Only show published books for public search
        query = query.Where(b => b.Status == BookStatus.Published);

        return query;
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        await _context.Books.AddAsync(book, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        _context.Books.Update(book);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
