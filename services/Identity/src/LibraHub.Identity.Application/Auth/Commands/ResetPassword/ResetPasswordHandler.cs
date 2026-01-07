using LibraHub.BuildingBlocks.Abstractions;
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
    IUnitOfWork unitOfWork,
    ILogger<ResetPasswordHandler> logger) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var token = await tokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (token == null)
        {
            logger.LogWarning("Password reset attempted with invalid token: {Token}", request.Token);
            return Result.Failure(Error.NotFound("Invalid or expired reset token"));
        }

        if (!token.IsValid)
        {
            logger.LogWarning("Password reset attempted with expired or used token. Token ID: {TokenId}, IsExpired: {IsExpired}, IsUsed: {IsUsed}",
                token.Id, token.IsExpired, token.IsUsed);
            return Result.Failure(Error.Validation("Reset token has expired or has already been used"));
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            logger.LogError("User not found for password reset token. Token ID: {TokenId}, UserId: {UserId}",
                token.Id, token.UserId);
            return Result.Failure(Error.NotFound("User not found"));
        }

        if (user.Status != UserStatus.Active)
        {
            logger.LogWarning("Password reset attempted for inactive user. UserId: {UserId}, Status: {Status}",
                user.Id, user.Status);
            return Result.Failure(Error.Validation("Cannot reset password for inactive user"));
        }

        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);

        user.UpdatePassword(newPasswordHash);
        token.MarkAsUsed();

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await tokenRepository.UpdateAsync(token, ct);
                await userRepository.UpdateAsync(user, ct);
            }, cancellationToken);

            logger.LogInformation("Password reset successful for user: {UserId}, Email: {Email}", user.Id, user.Email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset password for user: {UserId}", user.Id);
            throw;
        }
    }
}
