using LibraHub.Content.Domain.Access;

namespace LibraHub.Content.Application.Abstractions;

public interface IAccessGrantRepository
{
    Task<AccessGrant?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task AddAsync(AccessGrant grant, CancellationToken cancellationToken = default);

    Task UpdateAsync(AccessGrant grant, CancellationToken cancellationToken = default);
}
