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
    IEntitlementRepository entitlementRepository,
    BuildingBlocks.Abstractions.IOutboxWriter outboxWriter) : IRequestHandler<AdminGrantEntitlementCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdminGrantEntitlementCommand request, CancellationToken cancellationToken)
    {
        var existing = await entitlementRepository.GetByUserAndBookAsync(
            request.UserId,
            request.BookId,
            cancellationToken);

        if (existing != null)
        {
            if (existing.IsActive)
            {
                return Result.Failure<Guid>(Error.Validation(LibraryErrors.Entitlement.AlreadyExists));
            }

            existing.Reactivate();
            await entitlementRepository.UpdateAsync(existing, cancellationToken);

            await outboxWriter.WriteAsync(
                new EntitlementGrantedV1
                {
                    UserId = existing.UserId,
                    BookId = existing.BookId,
                    Source = existing.Source.ToString(),
                    AcquiredAtUtc = new DateTimeOffset(existing.AcquiredAt, TimeSpan.Zero)
                },
                EventTypes.EntitlementGranted,
                cancellationToken);

            return Result.Success(existing.Id);
        }

        var entitlement = new Entitlement(
            Guid.NewGuid(),
            request.UserId,
            request.BookId,
            EntitlementSource.AdminGrant);

        await entitlementRepository.AddAsync(entitlement, cancellationToken);
        await outboxWriter.WriteAsync(
            new EntitlementGrantedV1
            {
                UserId = entitlement.UserId,
                BookId = entitlement.BookId,
                Source = entitlement.Source.ToString(),
                AcquiredAtUtc = new DateTimeOffset(entitlement.AcquiredAt, TimeSpan.Zero)
            },
            EventTypes.EntitlementGranted,
            cancellationToken);

        return Result.Success(entitlement.Id);
    }
}
