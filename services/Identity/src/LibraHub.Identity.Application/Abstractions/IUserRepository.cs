using LibraHub.Identity.Domain.Users;

namespace LibraHub.Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<int> CountAdminsAsync(CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetUsersPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetRemovedUsersPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountRemovedAsync(CancellationToken cancellationToken = default);

    Task<UserStatisticsResult> GetStatisticsAsync(DateTime last30Days, DateTime last7Days, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
