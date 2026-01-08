using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Me.Queries.GetMe;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<GetMeResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetMeQueryHandler> _logger;

    public GetMeQueryHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        ILogger<GetMeQueryHandler> logger)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<GetMeResponseDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            _logger.LogWarning("Invalid or missing user ID in token");
            return Result.Failure<GetMeResponseDto>(Error.Unauthorized("Invalid or missing user identifier in token"));
        }

        var userId = _currentUser.UserId.Value;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return Result.Failure<GetMeResponseDto>(Error.Unauthorized("User not found"));
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("User account is not active: {UserId}, Status: {Status}", userId, user.Status);
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
            Avatar = user.Avatar,
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
