using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Entitlements;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Library.Application.Entitlements.Queries.GetAllEntitlements;

public class GetAllEntitlementsHandler(
    IEntitlementRepository entitlementRepository,
    IIdentityClient identityClient,
    IBookSnapshotStore bookSnapshotStore) : IRequestHandler<GetAllEntitlementsQuery, Result<GetAllEntitlementsResponseDto>>
{
    public async Task<Result<GetAllEntitlementsResponseDto>> Handle(GetAllEntitlementsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1)
        {
            return Result.Failure<GetAllEntitlementsResponseDto>(Error.Validation("Page must be greater than 0"));
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return Result.Failure<GetAllEntitlementsResponseDto>(Error.Validation("PageSize must be between 1 and 100"));
        }

        EntitlementStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<EntitlementStatus>(request.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }
            else
            {
                return Result.Failure<GetAllEntitlementsResponseDto>(Error.Validation($"Invalid status: {request.Status}. Valid values are: Active, Revoked"));
            }
        }

        EntitlementSource? source = null;
        if (!string.IsNullOrWhiteSpace(request.Source))
        {
            if (Enum.TryParse<EntitlementSource>(request.Source, true, out var parsedSource))
            {
                source = parsedSource;
            }
            else
            {
                return Result.Failure<GetAllEntitlementsResponseDto>(Error.Validation($"Invalid source: {request.Source}. Valid values are: Purchase, AdminGrant, Promotion"));
            }
        }

        DateTime? fromDate = null;
        if (!string.IsNullOrWhiteSpace(request.Period))
        {
            fromDate = request.Period.ToLower() switch
            {
                "24h" => DateTime.UtcNow.AddHours(-24),
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                _ => null
            };
        }

        var skip = (request.Page - 1) * request.PageSize;
        var entitlements = await entitlementRepository.GetAllAsync(
            skip,
            request.PageSize,
            request.UserId,
            request.BookId,
            status,
            source,
            fromDate,
            cancellationToken);

        var totalCount = await entitlementRepository.CountAllAsync(
            request.UserId,
            request.BookId,
            status,
            source,
            fromDate,
            cancellationToken);

        var uniqueUserIds = entitlements.Select(e => e.UserId).Distinct().ToList();
        var userInfoDict = await identityClient.GetUsersByIdsAsync(uniqueUserIds, cancellationToken);

        var uniqueBookIds = entitlements.Select(e => e.BookId).Distinct().ToList();
        var bookSnapshotDict = new Dictionary<Guid, Domain.Books.BookSnapshot?>();

        foreach (var bookId in uniqueBookIds)
        {
            var bookSnapshot = await bookSnapshotStore.GetByIdAsync(bookId, cancellationToken);
            bookSnapshotDict[bookId] = bookSnapshot;
        }

        var response = new GetAllEntitlementsResponseDto
        {
            Entitlements = entitlements.Select(e =>
            {
                var userInfo = userInfoDict.GetValueOrDefault(e.UserId);
                var bookSnapshot = bookSnapshotDict.GetValueOrDefault(e.BookId);
                return new EntitlementSummaryDto
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    UserDisplayName = userInfo?.DisplayName,
                    UserEmail = userInfo?.Email,
                    BookId = e.BookId,
                    BookTitle = bookSnapshot?.Title,
                    Status = e.Status.ToString(),
                    Source = e.Source.ToString(),
                    AcquiredAt = e.AcquiredAt,
                    RevokedAt = e.RevokedAt,
                    RevocationReason = e.RevocationReason,
                    OrderId = e.OrderId
                };
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}

