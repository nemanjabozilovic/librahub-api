using LibraHub.Library.Domain.Entitlements;

namespace LibraHub.Library.Application.Abstractions;

public interface IEntitlementRepository
{
    Task<Entitlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Entitlement?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);

    Task<List<Entitlement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<Entitlement>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<Entitlement>> GetActiveByUserIdPagedAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<List<Entitlement>> GetAllAsync(
        int skip,
        int take,
        Guid? userId = null,
        Guid? bookId = null,
        EntitlementStatus? status = null,
        EntitlementSource? source = null,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(
        Guid? userId = null,
        Guid? bookId = null,
        EntitlementStatus? status = null,
        EntitlementSource? source = null,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default);

    Task<EntitlementStatisticsResult> GetStatisticsAsync(DateTime last30Days, CancellationToken cancellationToken = default);

    Task<bool> HasAccessAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);

    Task AddAsync(Entitlement entitlement, CancellationToken cancellationToken = default);

    Task UpdateAsync(Entitlement entitlement, CancellationToken cancellationToken = default);
}

