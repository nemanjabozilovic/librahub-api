using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.ResetPassword;

public class ResetPasswordHandler(
    IPasswordResetTokenRepository tokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<ResetPasswordHandler> logger) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate token exists
        var token = await tokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (token == null)
        {
            logger.LogWarning("Password reset attempted with invalid token: {Token}", request.Token);
            return Result.Failure(Error.NotFound("Invalid or expired reset token"));
        }

        // Validate token is still valid (not expired and not used)
        if (!token.IsValid)
        {
            logger.LogWarning("Password reset attempted with expired or used token. Token ID: {TokenId}, IsExpired: {IsExpired}, IsUsed: {IsUsed}",
                token.Id, token.IsExpired, token.IsUsed);
            return Result.Failure(Error.Validation("Reset token has expired or has already been used"));
        }

        // Get user
        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            logger.LogError("User not found for password reset token. Token ID: {TokenId}, UserId: {UserId}",
                token.Id, token.UserId);
            return Result.Failure(Error.NotFound("User not found"));
        }

        // Check if user is active
        if (user.Status != UserStatus.Active)
        {
            logger.LogWarning("Password reset attempted for inactive user. UserId: {UserId}, Status: {Status}",
                user.Id, user.Status);
            return Result.Failure(Error.Validation("Cannot reset password for inactive user"));
        }

        // Hash new password
        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);

        // Update user password using domain method
        user.UpdatePassword(newPasswordHash);

        // Mark token as used
        token.MarkAsUsed();

        // Save changes
        await tokenRepository.UpdateAsync(token, cancellationToken);
        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Password reset successful for user: {UserId}, Email: {Email}", user.Id, user.Email);
        return Result.Success();
    }
}

