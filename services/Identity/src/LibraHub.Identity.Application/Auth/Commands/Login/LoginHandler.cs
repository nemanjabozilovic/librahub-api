using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Auth.Dtos;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.Login;

public class LoginHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IRecaptchaService recaptchaService,
    IClock clock,
    IOptions<SecurityOptions> securityOptions) : IRequestHandler<LoginCommand, Result<AuthTokensDto>>
{
    public async Task<Result<AuthTokensDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecaptchaToken))
        {
            return Result.Failure<AuthTokensDto>(Error.Validation("reCAPTCHA token is required"));
        }

        var isRecaptchaValid = await recaptchaService.VerifyAsync(request.RecaptchaToken, cancellationToken);
        if (!isRecaptchaValid)
        {
            return Result.Failure<AuthTokensDto>(Error.Validation("reCAPTCHA verification failed"));
        }

        var emailLower = request.Email.ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(emailLower, cancellationToken);

        if (user == null)
        {
            return Result.Failure<AuthTokensDto>(Error.Unauthorized("Invalid credentials"));
        }

        if (user.Status == UserStatus.Removed)
        {
            return Result.Failure<AuthTokensDto>(Error.Forbidden("Account is removed"));
        }

        if (user.IsLockedOut(clock.UtcNow))
        {
            return Result.Failure<AuthTokensDto>(Error.Forbidden("Account is locked. Please try again later."));
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            var maxAttempts = securityOptions.Value.MaxFailedLoginAttempts;
            var lockoutDuration = TimeSpan.FromMinutes(securityOptions.Value.LockoutDurationMinutes);
            user.RecordFailedLogin(maxAttempts, lockoutDuration);
            await userRepository.UpdateAsync(user, cancellationToken);
            return Result.Failure<AuthTokensDto>(Error.Unauthorized("Invalid credentials"));
        }

        user.RecordSuccessfulLogin();
        await userRepository.UpdateAsync(user, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenValue = tokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = tokenService.GetRefreshTokenExpiration();

        var refreshToken = new Domain.Tokens.RefreshToken(
            Guid.NewGuid(),
            user.Id,
            refreshTokenValue,
            refreshTokenExpiresAt.UtcDateTime);

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return Result.Success(new AuthTokensDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = refreshTokenExpiresAt
        });
    }
}
