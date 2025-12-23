using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Identity.Infrastructure.Repositories;

public class RegistrationCompletionTokenRepository : IRegistrationCompletionTokenRepository
{
    private readonly IdentityDbContext _context;

    public RegistrationCompletionTokenRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<RegistrationCompletionToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RegistrationCompletionTokens
            .FirstOrDefaultAsync(rct => rct.Token == token, cancellationToken);
    }

    public async Task AddAsync(RegistrationCompletionToken token, CancellationToken cancellationToken = default)
    {
        await _context.RegistrationCompletionTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RegistrationCompletionToken token, CancellationToken cancellationToken = default)
    {
        _context.RegistrationCompletionTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

