using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Library.V1;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Library.Application.Entitlements.Commands.RevokeEntitlement;

public class RevokeEntitlementHandler(
    IEntitlementRepository entitlementRepository,
    BuildingBlocks.Abstractions.IOutboxWriter outboxWriter) : IRequestHandler<RevokeEntitlementCommand, Result>
{
    public async Task<Result> Handle(RevokeEntitlementCommand request, CancellationToken cancellationToken)
    {
        var entitlement = await entitlementRepository.GetByIdAsync(request.EntitlementId, cancellationToken);
        if (entitlement == null)
        {
            return Result.Failure(Error.NotFound(LibraryErrors.Entitlement.NotFound));
        }

        if (!entitlement.IsActive)
        {
            return Result.Failure(Error.Validation(LibraryErrors.Entitlement.AlreadyRevoked));
        }

        entitlement.Revoke(request.Reason);
        await entitlementRepository.UpdateAsync(entitlement, cancellationToken);

        await outboxWriter.WriteAsync(
            new EntitlementRevokedV1
            {
                UserId = entitlement.UserId,
                BookId = entitlement.BookId,
                Reason = request.Reason ?? "Manual revocation",
                RevokedAtUtc = new DateTimeOffset(entitlement.RevokedAt!.Value, TimeSpan.Zero)
            },
            EventTypes.EntitlementRevoked,
            cancellationToken);

        return Result.Success();
    }
}
