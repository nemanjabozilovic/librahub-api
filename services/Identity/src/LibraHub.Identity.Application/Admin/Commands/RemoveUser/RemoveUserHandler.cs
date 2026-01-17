using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.BuildingBlocks.Urls;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Admin.Commands.RemoveUser;

public class RemoveUserHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IOutboxWriter outboxWriter,
    IClock clock,
    IObjectStorage objectStorage,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    ILogger<RemoveUserHandler> logger) : IRequestHandler<RemoveUserCommand, Result>
{
    public async Task<Result> Handle(RemoveUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        var adminCheck = await EnsureNotLastAdminAsync(user, cancellationToken);
        if (adminCheck.IsFailure)
        {
            return adminCheck;
        }

        await TryDeleteAvatarAsync(user.Avatar, cancellationToken);

        var integrationEvent = new UserRemovedV1
        {
            UserId = user.Id,
            Reason = request.Reason,
            OccurredAt = clock.UtcNowOffset
        };

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            user.Remove(request.Reason);
            await userRepository.UpdateAsync(user, ct);

            await refreshTokenRepository.RevokeAllForUserAsync(user.Id, ct);

            await outboxWriter.WriteAsync(integrationEvent, EventTypes.UserRemoved, ct);
        }, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> EnsureNotLastAdminAsync(Domain.Users.User user, CancellationToken cancellationToken)
    {
        if (!user.IsAdmin())
        {
            return Result.Success();
        }

        var adminCount = await userRepository.CountAdminsAsync(cancellationToken);
        if (adminCount <= 1)
        {
            return Result.Failure(Error.Validation("Cannot remove the last admin user"));
        }

        return Result.Success();
    }

    private async Task TryDeleteAvatarAsync(string? avatarUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return;
        }

        var bucketName = configuration["Storage:AvatarsBucketName"];
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return;
        }

        var objectKey = UrlPathExtractor.GetPathAfterSegment(avatarUrl, "avatar");
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return;
        }

        try
        {
            await objectStorage.DeleteAsync(bucketName, objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete avatar for user {AvatarUrl}", avatarUrl);
        }
    }
}
