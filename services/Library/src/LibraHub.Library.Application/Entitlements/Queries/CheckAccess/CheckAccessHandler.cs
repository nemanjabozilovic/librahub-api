using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using MediatR;

namespace LibraHub.Library.Application.Entitlements.Queries.CheckAccess;

public class CheckAccessHandler(
    IEntitlementRepository entitlementRepository) : IRequestHandler<CheckAccessQuery, Result<CheckAccessDto>>
{
    public async Task<Result<CheckAccessDto>> Handle(CheckAccessQuery request, CancellationToken cancellationToken)
    {
        var entitlement = await entitlementRepository.GetByUserAndBookAsync(
            request.UserId,
            request.BookId,
            cancellationToken);

        if (entitlement == null)
        {
            return Result.Success(new CheckAccessDto
            {
                HasAccess = false,
                Status = "None"
            });
        }

        return Result.Success(new CheckAccessDto
        {
            HasAccess = entitlement.IsActive,
            Status = entitlement.IsActive ? "Active" : "Revoked"
        });
    }
}
