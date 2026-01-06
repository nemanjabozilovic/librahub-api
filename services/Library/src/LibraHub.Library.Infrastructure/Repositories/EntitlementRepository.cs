using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Entitlements;
using LibraHub.Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Library.Infrastructure.Repositories;

public class EntitlementRepository : IEntitlementRepository
{
    private readonly LibraryDbContext _context;

    public EntitlementRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Entitlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Entitlement?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements
            .FirstOrDefaultAsync(e => e.UserId == userId && e.BookId == bookId, cancellationToken);
    }

    public async Task<List<Entitlement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.AcquiredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Entitlement>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements
            .Where(e => e.UserId == userId && e.Status == EntitlementStatus.Active)
            .OrderByDescending(e => e.AcquiredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Entitlement>> GetActiveByUserIdPagedAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements
            .Where(e => e.UserId == userId && e.Status == EntitlementStatus.Active)
            .OrderByDescending(e => e.AcquiredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements
            .CountAsync(e => e.UserId == userId && e.Status == EntitlementStatus.Active, cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements.CountAsync(cancellationToken);
    }

    public async Task<List<Entitlement>> GetAllAsync(
        int skip,
        int take,
        Guid? userId = null,
        Guid? bookId = null,
        EntitlementStatus? status = null,
        EntitlementSource? source = null,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Entitlements.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId.Value);
        }

        if (bookId.HasValue)
        {
            query = query.Where(e => e.BookId == bookId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(e => e.Source == source.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.AcquiredAt >= fromDate.Value);
        }

        return await query
            .OrderByDescending(e => e.AcquiredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(
        Guid? userId = null,
        Guid? bookId = null,
        EntitlementStatus? status = null,
        EntitlementSource? source = null,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Entitlements.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId.Value);
        }

        if (bookId.HasValue)
        {
            query = query.Where(e => e.BookId == bookId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(e => e.Source == source.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.AcquiredAt >= fromDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<EntitlementStatisticsResult> GetStatisticsAsync(DateTime last30Days, CancellationToken cancellationToken = default)
    {
        var total = await _context.Entitlements.CountAsync(cancellationToken);
        var active = await _context.Entitlements.CountAsync(e => e.Status == EntitlementStatus.Active, cancellationToken);
        var revoked = await _context.Entitlements.CountAsync(e => e.Status == EntitlementStatus.Revoked, cancellationToken);
        var grantedLast30Days = await _context.Entitlements.CountAsync(e => e.AcquiredAt >= last30Days, cancellationToken);

        return new EntitlementStatisticsResult
        {
            Total = total,
            Active = active,
            Revoked = revoked,
            GrantedLast30Days = grantedLast30Days
        };
    }

    public async Task<bool> HasAccessAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.Entitlements
            .AnyAsync(e => e.UserId == userId && e.BookId == bookId && e.Status == EntitlementStatus.Active, cancellationToken);
    }

    public async Task AddAsync(Entitlement entitlement, CancellationToken cancellationToken = default)
    {
        await _context.Entitlements.AddAsync(entitlement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Entitlement entitlement, CancellationToken cancellationToken = default)
    {
        _context.Entitlements.Update(entitlement);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

