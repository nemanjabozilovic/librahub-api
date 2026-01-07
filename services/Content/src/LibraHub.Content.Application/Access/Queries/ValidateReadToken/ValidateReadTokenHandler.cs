using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Domain.Access;
using LibraHub.Content.Domain.Errors;
using LibraHub.Content.Domain.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Content.Application.Access.Queries.ValidateReadToken;

public class ValidateReadTokenHandler(
    IAccessGrantRepository accessGrantRepository,
    IStoredObjectRepository storedObjectRepository,
    IBookEditionRepository editionRepository,
    ICoverRepository coverRepository,
    IClock clock,
    IOptions<ReadAccessOptions> readAccessOptions) : IRequestHandler<ValidateReadTokenQuery, Result<AccessGrantInfo>>
{
    public async Task<Result<AccessGrantInfo>> Handle(ValidateReadTokenQuery request, CancellationToken cancellationToken)
    {
        var grant = await accessGrantRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (grant == null)
        {
            return Result.Failure<AccessGrantInfo>(Error.NotFound(ContentErrors.Access.TokenInvalid));
        }

        if (!grant.IsValid(clock.UtcNow))
        {
            if (grant.IsRevoked)
            {
                return Result.Failure<AccessGrantInfo>(Error.Validation(ContentErrors.Access.TokenRevoked));
            }
            return Result.Failure<AccessGrantInfo>(Error.Validation(ContentErrors.Access.TokenExpired));
        }

        var refreshThreshold = TimeSpan.FromMinutes(readAccessOptions.Value.TokenRefreshThresholdMinutes);
        if (grant.IsNearExpiry(clock.UtcNow, refreshThreshold))
        {
            var newExpiresAt = clock.UtcNow.AddMinutes(readAccessOptions.Value.TokenExpirationMinutes);
            grant.RefreshExpiry(newExpiresAt);
            await accessGrantRepository.UpdateAsync(grant, cancellationToken);
        }

        StoredObject? storedObject = null;

        if (grant.Scope == AccessScope.Cover)
        {
            var cover = await coverRepository.GetByBookIdAsync(grant.BookId, cancellationToken);
            if (cover == null || !cover.IsAccessible)
            {
                return Result.Failure<AccessGrantInfo>(Error.NotFound(ContentErrors.Cover.NotFound));
            }

            storedObject = await storedObjectRepository.GetByIdAsync(cover.StoredObjectId, cancellationToken);
        }
        else if (grant.Scope == AccessScope.Edition)
        {
            if (!grant.Format.HasValue)
            {
                return Result.Failure<AccessGrantInfo>(Error.Validation(ContentErrors.Edition.InvalidFormat));
            }

            if (!grant.Version.HasValue)
            {
                return Result.Failure<AccessGrantInfo>(Error.Validation(ContentErrors.Edition.InvalidVersion));
            }

            var edition = await editionRepository.GetByBookIdFormatAndVersionAsync(
                grant.BookId, grant.Format.Value, grant.Version.Value, cancellationToken);
            if (edition == null || !edition.IsAccessible)
            {
                return Result.Failure<AccessGrantInfo>(Error.NotFound(ContentErrors.Edition.NotFound));
            }

            storedObject = await storedObjectRepository.GetByIdAsync(edition.StoredObjectId, cancellationToken);
        }

        if (storedObject == null || !storedObject.IsAccessible)
        {
            return Result.Failure<AccessGrantInfo>(Error.NotFound(ContentErrors.Storage.DownloadFailed));
        }

        return Result.Success(new AccessGrantInfo
        {
            BookId = grant.BookId,
            Format = grant.Format?.ToString(),
            Version = grant.Version,
            Scope = grant.Scope.ToString(),
            ObjectKey = storedObject.ObjectKey,
            ContentType = storedObject.ContentType,
            SizeBytes = storedObject.SizeBytes
        });
    }
}
