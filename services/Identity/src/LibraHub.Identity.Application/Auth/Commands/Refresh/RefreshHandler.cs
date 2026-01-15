using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Dtos;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.Refresh;

public class RefreshHandler : IRequestHandler<RefreshCommand, Result<AuthTokensDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public RefreshHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IClock clock)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<Result<AuthTokensDto>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Result.Failure<AuthTokensDto>(Error.Unauthorized("Invalid refresh token"));
        }

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user == null || user.Status == Domain.Users.UserStatus.Removed)
        {
            return Result.Failure<AuthTokensDto>(Error.Forbidden("User account is removed"));
        }

        refreshToken.Revoke();
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = _tokenService.GetRefreshTokenExpiration();

        var newRefreshToken = new Domain.Tokens.RefreshToken(
            Guid.NewGuid(),
            user.Id,
            newRefreshTokenValue,
            refreshTokenExpiresAt.UtcDateTime);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return Result.Success(new AuthTokensDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt
        });
    }
}
