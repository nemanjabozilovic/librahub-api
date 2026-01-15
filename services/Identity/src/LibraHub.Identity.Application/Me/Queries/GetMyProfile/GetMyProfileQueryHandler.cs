using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Me.Queries.GetMyProfile;

public class GetMyProfileQueryHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IOptions<IdentityOptions> identityOptions,
    ILogger<GetMyProfileQueryHandler> logger) : IRequestHandler<GetMyProfileQuery, Result<UserProfileDto>>
{
    private readonly IdentityOptions _identityOptions = identityOptions.Value;

    public async Task<Result<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId();
        if (userIdResult.IsFailure)
        {
            return Result.Failure<UserProfileDto>(userIdResult.Error!);
        }

        var user = await userRepository.GetByIdAsync(userIdResult.Value, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found: {UserId}", userIdResult.Value);
            return Result.Failure<UserProfileDto>(Error.Unauthorized("User not found"));
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<UserProfileDto>(Error.Forbidden("Account is removed"));
        }

        return Result.Success(new UserProfileDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = new DateTimeOffset(user.DateOfBirth, TimeSpan.Zero),
            Phone = user.Phone,
            Avatar = BuildAvatarUrl(user.Avatar, user.Id),
            EmailAnnouncementsEnabled = user.EmailAnnouncementsEnabled,
            EmailPromotionsEnabled = user.EmailPromotionsEnabled
        });
    }

    private string? BuildAvatarUrl(string? avatar, Guid userId)
    {
        var relative = NormalizeAvatarPath(avatar, userId);
        if (string.IsNullOrWhiteSpace(relative))
        {
            return null;
        }

        return $"{_identityOptions.GatewayBaseUrl.TrimEnd('/')}{relative}";
    }

    private static string? NormalizeAvatarPath(string? avatar, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(avatar))
        {
            return null;
        }

        var path = avatar.Trim();

        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                path = uri.AbsolutePath;
            }
        }

        var marker = "/avatar/";
        var idx = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        var after = path[(idx + marker.Length)..];
        var fileName = Path.GetFileName(after);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        return $"/api/users/{userId}/avatar/{fileName}";
    }
}

