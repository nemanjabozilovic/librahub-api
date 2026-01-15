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

public class RemoveUserHandler : IRequestHandler<RemoveUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IClock _clock;
    private readonly IObjectStorage _objectStorage;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveUserHandler> _logger;

    public RemoveUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxWriter outboxWriter,
        IClock clock,
        IObjectStorage objectStorage,
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        ILogger<RemoveUserHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _outboxWriter = outboxWriter;
        _clock = clock;
        _objectStorage = objectStorage;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
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
            OccurredAt = _clock.UtcNowOffset
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            user.Remove(request.Reason);
            await _userRepository.UpdateAsync(user, ct);

            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, ct);

            await _outboxWriter.WriteAsync(integrationEvent, EventTypes.UserRemoved, ct);
        }, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> EnsureNotLastAdminAsync(Domain.Users.User user, CancellationToken cancellationToken)
    {
        if (!user.IsAdmin())
        {
            return Result.Success();
        }

        var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
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

        var bucketName = _configuration["Storage:AvatarsBucketName"];
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
            await _objectStorage.DeleteAsync(bucketName, objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete avatar for user {AvatarUrl}", avatarUrl);
        }
    }
}


