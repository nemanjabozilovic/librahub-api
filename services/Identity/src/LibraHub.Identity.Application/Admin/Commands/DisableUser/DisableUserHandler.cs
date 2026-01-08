using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Admin.Commands.DisableUser;

public class DisableUserHandler : IRequestHandler<DisableUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IClock _clock;
    private readonly IObjectStorage _objectStorage;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DisableUserHandler> _logger;

    public DisableUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxWriter outboxWriter,
        IClock clock,
        IObjectStorage objectStorage,
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        ILogger<DisableUserHandler> logger)
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

    public async Task<Result> Handle(DisableUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        if (user.IsAdmin())
        {
            var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
            if (adminCount <= 1)
            {
                return Result.Failure(Error.Validation("Cannot remove the last admin user"));
            }
        }

        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            try
            {
                var bucketName = _configuration["Storage:AvatarsBucketName"];
                if (!string.IsNullOrWhiteSpace(bucketName))
                {
                    var objectKey = ExtractObjectKeyFromUrl(user.Avatar);
                    if (!string.IsNullOrWhiteSpace(objectKey))
                    {
                        await _objectStorage.DeleteAsync(bucketName, objectKey, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete avatar for user {UserId}", request.UserId);
            }
        }

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

    private static string? ExtractObjectKeyFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var avatarIndex = Array.IndexOf(pathParts, "avatar");
            if (avatarIndex >= 0 && avatarIndex < pathParts.Length - 1)
            {
                return string.Join("/", pathParts.Skip(avatarIndex + 1));
            }
        }
        catch
        {
            // Invalid URL format
        }

        return null;
    }
}
