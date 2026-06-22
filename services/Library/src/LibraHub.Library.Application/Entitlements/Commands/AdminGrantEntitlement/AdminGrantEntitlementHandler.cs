using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Library.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Entitlements;
using LibraHub.Library.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Library.Application.Entitlements.Commands.AdminGrantEntitlement;

public class AdminGrantEntitlementHandler(
    EntitlementGrantService entitlementGrantService,
    IBookSnapshotStore bookSnapshotStore,
    BuildingBlocks.Abstractions.IOutboxWriter outboxWriter) : IRequestHandler<AdminGrantEntitlementCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdminGrantEntitlementCommand request, CancellationToken cancellationToken)
    {
        var (entitlement, outcome) = await entitlementGrantService.GrantOrReactivateAsync(
            request.UserId,
            request.BookId,
            EntitlementSource.AdminGrant,
            null,
            cancellationToken);

        if (outcome == EntitlementGrantOutcome.AlreadyActive)
        {
            return Result.Failure<Guid>(Error.Validation(LibraryErrors.Entitlement.AlreadyExists));
        }

        var snapshot = await bookSnapshotStore.GetByIdAsync(entitlement.BookId, cancellationToken);

        await outboxWriter.WriteAsync(
            new EntitlementGrantedV1
            {
                UserId = entitlement.UserId,
                BookId = entitlement.BookId,
                Source = entitlement.Source.ToString(),
                AcquiredAtUtc = new DateTimeOffset(entitlement.AcquiredAt, TimeSpan.Zero),
                BookTitle = snapshot?.Title ?? string.Empty
            },
            EventTypes.EntitlementGranted,
            cancellationToken);

        return Result.Success(entitlement.Id);
    }
}
