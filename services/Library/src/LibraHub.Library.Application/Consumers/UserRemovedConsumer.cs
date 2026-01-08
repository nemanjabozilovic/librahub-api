using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Library.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Library.Application.Consumers;

public class UserRemovedConsumer(
    IEntitlementRepository entitlementRepository,
    IReadingProgressRepository readingProgressRepository,
    IUnitOfWork unitOfWork,
    ILogger<UserRemovedConsumer> logger)
{
    public async Task HandleAsync(UserRemovedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing UserRemoved event for UserId: {UserId}, Reason: {Reason}", @event.UserId, @event.Reason);

        var entitlements = await entitlementRepository.GetByUserIdAsync(@event.UserId, cancellationToken);

        if (entitlements.Count == 0)
        {
            logger.LogInformation("No entitlements found for UserId: {UserId}", @event.UserId);
            return;
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var entitlement in entitlements)
            {
                if (entitlement.IsActive)
                {
                    entitlement.Revoke($"User removed: {@event.Reason}");
                    await entitlementRepository.UpdateAsync(entitlement, ct);
                }
            }
        }, cancellationToken);

        logger.LogInformation("Revoked {Count} entitlements for UserId: {UserId}", entitlements.Count(e => e.IsActive), @event.UserId);

        var readingProgress = await readingProgressRepository.GetByUserIdAsync(@event.UserId, cancellationToken);

        if (readingProgress.Count > 0)
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var progress in readingProgress)
                {
                    await readingProgressRepository.DeleteAsync(progress, ct);
                }
            }, cancellationToken);

            logger.LogInformation("Deleted {Count} reading progress records for UserId: {UserId}", readingProgress.Count, @event.UserId);
        }
    }
}

