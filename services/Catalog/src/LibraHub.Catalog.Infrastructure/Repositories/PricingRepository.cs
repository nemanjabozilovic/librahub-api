using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Catalog.Infrastructure.Repositories;

public class PricingRepository : IPricingRepository
{
    private readonly CatalogDbContext _context;

    public PricingRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<PricingPolicy?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await _context.PricingPolicies
            .FirstOrDefaultAsync(p => p.BookId == bookId, cancellationToken);
    }

    public async Task<Dictionary<Guid, PricingPolicy>> GetByBookIdsAsync(IEnumerable<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        var bookIdsList = bookIds.ToList();
        if (bookIdsList.Count == 0)
        {
            return new Dictionary<Guid, PricingPolicy>();
        }

        var policies = await _context.PricingPolicies
            .Where(p => bookIdsList.Contains(p.BookId))
            .ToListAsync(cancellationToken);

        return policies.ToDictionary(p => p.BookId);
    }

    public async Task AddAsync(PricingPolicy pricingPolicy, CancellationToken cancellationToken = default)
    {
        await _context.PricingPolicies.AddAsync(pricingPolicy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PricingPolicy pricingPolicy, CancellationToken cancellationToken = default)
    {
        _context.PricingPolicies.Update(pricingPolicy);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
