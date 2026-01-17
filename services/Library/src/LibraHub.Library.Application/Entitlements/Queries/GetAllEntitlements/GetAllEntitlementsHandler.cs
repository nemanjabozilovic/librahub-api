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

        var statusResult = ParseEntitlementStatus(request.Status);
        if (statusResult.IsFailure)
        {
            return Result.Failure<GetAllEntitlementsResponseDto>(statusResult.Error!);
        }

        var sourceResult = ParseEntitlementSource(request.Source);
        if (sourceResult.IsFailure)
        {
            return Result.Failure<GetAllEntitlementsResponseDto>(sourceResult.Error!);
        }

        var fromDate = ParsePeriod(request.Period);

        var skip = (request.Page - 1) * request.PageSize;
        var entitlements = await entitlementRepository.GetAllAsync(
            skip,
            request.PageSize,
            request.UserId,
            request.BookId,
            statusResult.Value,
            sourceResult.Value,
            fromDate,
            cancellationToken);

        var totalCount = await entitlementRepository.CountAllAsync(
            request.UserId,
            request.BookId,
            statusResult.Value,
            sourceResult.Value,
            fromDate,
            cancellationToken);

        var uniqueUserIds = entitlements.Select(e => e.UserId).Distinct().ToList();
        var userInfoDictResult = await identityClient.GetUsersByIdsAsync(uniqueUserIds, cancellationToken);
        var userInfoDict = userInfoDictResult.IsSuccess
            ? userInfoDictResult.Value
            : new Dictionary<Guid, UserInfo?>();

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
                    AcquiredAt = new DateTimeOffset(e.AcquiredAt, TimeSpan.Zero),
                    RevokedAt = e.RevokedAt.HasValue ? new DateTimeOffset(e.RevokedAt.Value, TimeSpan.Zero) : null,
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

    private static Result<EntitlementStatus?> ParseEntitlementStatus(string? statusValue)
    {
        if (string.IsNullOrWhiteSpace(statusValue))
        {
            return Result.Success<EntitlementStatus?>(null);
        }

        if (Enum.TryParse<EntitlementStatus>(statusValue, true, out var parsedStatus))
        {
            return Result.Success<EntitlementStatus?>(parsedStatus);
        }

        return Result.Failure<EntitlementStatus?>(Error.Validation($"Invalid status: {statusValue}. Valid values are: Active, Revoked"));
    }

    private static Result<EntitlementSource?> ParseEntitlementSource(string? sourceValue)
    {
        if (string.IsNullOrWhiteSpace(sourceValue))
        {
            return Result.Success<EntitlementSource?>(null);
        }

        if (Enum.TryParse<EntitlementSource>(sourceValue, true, out var parsedSource))
        {
            return Result.Success<EntitlementSource?>(parsedSource);
        }

        return Result.Failure<EntitlementSource?>(Error.Validation($"Invalid source: {sourceValue}. Valid values are: Purchase, AdminGrant, Promotion"));
    }

    private static DateTime? ParsePeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            return null;
        }

        return period.ToLower() switch
        {
            "24h" => DateTime.UtcNow.AddHours(-24),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _ => null
        };
    }
}
