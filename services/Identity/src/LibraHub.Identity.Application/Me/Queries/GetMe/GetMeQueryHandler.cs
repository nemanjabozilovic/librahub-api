using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Me.Queries.GetMe;

public class GetMeQueryHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IOptions<IdentityOptions> identityOptions,
    ILogger<GetMeQueryHandler> logger) : IRequestHandler<GetMeQuery, Result<GetMeResponseDto>>
{
    private readonly IdentityOptions _identityOptions = identityOptions.Value;

    public async Task<Result<GetMeResponseDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            logger.LogWarning("Invalid or missing user ID in token");
            return Result.Failure<GetMeResponseDto>(Error.Unauthorized("Invalid or missing user identifier in token"));
        }

        var userId = currentUser.UserId.Value;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found: {UserId}", userId);
            return Result.Failure<GetMeResponseDto>(Error.Unauthorized("User not found"));
        }

        if (user.Status != UserStatus.Active)
        {
            logger.LogWarning("User account is not active: {UserId}, Status: {Status}", userId, user.Status);
            return Result.Failure<GetMeResponseDto>(Error.Forbidden("Account is removed"));
        }

        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();

        var response = new GetMeResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Phone = user.Phone,
            Avatar = AvatarUrlHelper.BuildAvatarUrl(user.Avatar, user.Id, _identityOptions.GatewayBaseUrl),
            DateOfBirth = user.DateOfBirth != default ? new DateTimeOffset(user.DateOfBirth, TimeSpan.Zero) : null,
            Roles = roles,
            EmailVerified = user.EmailVerified,
            Status = user.Status.ToString(),
            CreatedAt = new DateTimeOffset(user.CreatedAt, TimeSpan.Zero),
            LastLoginAt = user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value, TimeSpan.Zero) : null
        };

        return Result.Success(response);
    }
}
