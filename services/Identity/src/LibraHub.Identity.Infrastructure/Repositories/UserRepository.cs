using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using LibraHub.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<int> CountAdminsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .CountAsync(ur => ur.Role == Role.Admin, cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetUsersPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .OrderBy(u => u.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserStatisticsResult> GetStatisticsAsync(DateTime last30Days, DateTime last7Days, CancellationToken cancellationToken = default)
    {
        var total = await _context.Users.CountAsync(cancellationToken);
        var active = await _context.Users.CountAsync(u => u.Status == UserStatus.Active, cancellationToken);
        var disabled = await _context.Users.CountAsync(u => u.Status == UserStatus.Disabled, cancellationToken);
        var pending = await _context.Users.CountAsync(u => !u.EmailVerified && u.Status == UserStatus.Active, cancellationToken);
        var newLast30Days = await _context.Users.CountAsync(u => u.CreatedAt >= last30Days, cancellationToken);
        var newLast7Days = await _context.Users.CountAsync(u => u.CreatedAt >= last7Days, cancellationToken);

        return new UserStatisticsResult
        {
            Total = total,
            Active = active,
            Disabled = disabled,
            Pending = pending,
            NewLast30Days = newLast30Days,
            NewLast7Days = newLast7Days
        };
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
