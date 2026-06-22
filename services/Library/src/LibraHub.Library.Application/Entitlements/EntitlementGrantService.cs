using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Entitlements;

namespace LibraHub.Library.Application.Entitlements;

public enum EntitlementGrantOutcome
{
    Created,
    Reactivated,
    AlreadyActive
}

public sealed class EntitlementGrantService(IEntitlementRepository entitlementRepository)
{
    public async Task<(Entitlement Entitlement, EntitlementGrantOutcome Outcome)> GrantOrReactivateAsync(
        Guid userId,
        Guid bookId,
        EntitlementSource source,
        Guid? orderId,
        CancellationToken cancellationToken)
    {
        var existing = await entitlementRepository.GetByUserAndBookAsync(userId, bookId, cancellationToken);

        if (existing != null)
        {
            if (existing.IsActive)
            {
                return (existing, EntitlementGrantOutcome.AlreadyActive);
            }

            existing.Reactivate();
            await entitlementRepository.UpdateAsync(existing, cancellationToken);
            return (existing, EntitlementGrantOutcome.Reactivated);
        }

        var entitlement = new Entitlement(Guid.NewGuid(), userId, bookId, source, orderId);
        await entitlementRepository.AddAsync(entitlement, cancellationToken);
        return (entitlement, EntitlementGrantOutcome.Created);
    }
}
