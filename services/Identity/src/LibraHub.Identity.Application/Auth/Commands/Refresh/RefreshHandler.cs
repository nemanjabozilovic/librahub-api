using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Dtos;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.Refresh;

public class RefreshHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService) : IRequestHandler<RefreshCommand, Result<AuthTokensDto>>
{
    public async Task<Result<AuthTokensDto>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Result.Failure<AuthTokensDto>(Error.Unauthorized("Invalid refresh token"));
        }

        var user = await userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user == null || user.Status == Domain.Users.UserStatus.Removed)
        {
            return Result.Failure<AuthTokensDto>(Error.Forbidden("User account is removed"));
        }

        refreshToken.Revoke();
        await refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = tokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = tokenService.GetRefreshTokenExpiration();

        var newRefreshToken = new Domain.Tokens.RefreshToken(
            Guid.NewGuid(),
            user.Id,
            newRefreshTokenValue,
            refreshTokenExpiresAt.UtcDateTime);

        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return Result.Success(new AuthTokensDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt
        });
    }
}
