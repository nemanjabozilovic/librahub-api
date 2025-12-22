using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Announcements;
using LibraHub.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Catalog.Infrastructure.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly CatalogDbContext _context;

    public AnnouncementRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<List<Announcement>> GetByBookIdAsync(Guid bookId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Announcements
            .Where(a => a.BookId == bookId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.Announcements
            .Where(a => a.BookId == bookId)
            .CountAsync(cancellationToken);
    }

    public async Task<List<Announcement>> GetPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Announcements
            .Where(a => a.Status == AnnouncementStatus.Published)
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Announcements
            .Where(a => a.Status == AnnouncementStatus.Published)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        await _context.Announcements.AddAsync(announcement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        _context.Announcements.Update(announcement);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
