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

    public async Task<List<Book>> SearchAsync(string? searchTerm, int page, int pageSize, bool includeAllStatuses = false, CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(searchTerm, includeAllStatuses);

        return await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSearchAsync(string? searchTerm, bool includeAllStatuses = false, CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(searchTerm, includeAllStatuses);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Books.CountAsync(cancellationToken);
    }

    public async Task<BookStatisticsResult> GetStatisticsAsync(DateTime last30Days, CancellationToken cancellationToken = default)
    {
        var total = await _context.Books.CountAsync(cancellationToken);
        var published = await _context.Books.CountAsync(b => b.Status == BookStatus.Published, cancellationToken);
        var draft = await _context.Books.CountAsync(b => b.Status == BookStatus.Draft, cancellationToken);
        var unlisted = await _context.Books.CountAsync(b => b.Status == BookStatus.Unlisted, cancellationToken);
        var newLast30Days = await _context.Books.CountAsync(b => b.CreatedAt >= last30Days, cancellationToken);

        return new BookStatisticsResult
        {
            Total = total,
            Published = published,
            Draft = draft,
            Unlisted = unlisted,
            NewLast30Days = newLast30Days
        };
    }

    private IQueryable<Book> BuildSearchQuery(string? searchTerm, bool includeAllStatuses = false)
    {
        var query = _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.Tags)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = $"%{searchTerm.ToLowerInvariant()}%";
            query = query.Where(b =>
                EF.Functions.ILike(b.Title, term) ||
                (b.Description != null && EF.Functions.ILike(b.Description, term)) ||
                b.Authors.Any(a => EF.Functions.ILike(a.Name, term)));
        }

        if (!includeAllStatuses)
        {
            query = query.Where(b => b.Status == BookStatus.Published);
        }
        else
        {
            query = query.Where(b => b.Status != BookStatus.Removed);
        }

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
