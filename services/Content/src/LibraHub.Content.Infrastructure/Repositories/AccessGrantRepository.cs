using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Domain.Access;
using LibraHub.Content.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Content.Infrastructure.Repositories;

public class AccessGrantRepository : IAccessGrantRepository
{
    private readonly ContentDbContext _context;

    public AccessGrantRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<AccessGrant?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.AccessGrants
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task AddAsync(AccessGrant grant, CancellationToken cancellationToken = default)
    {
        await _context.AccessGrants.AddAsync(grant, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AccessGrant grant, CancellationToken cancellationToken = default)
    {
        _context.AccessGrants.Update(grant);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
