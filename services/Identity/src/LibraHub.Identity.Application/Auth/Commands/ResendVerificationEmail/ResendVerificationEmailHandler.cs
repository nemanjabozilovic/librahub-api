using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Constants;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Tokens;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.ResendVerificationEmail;

public class ResendVerificationEmailHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository tokenRepository,
    IEmailVerificationTokenService tokenService,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<ResendVerificationEmailHandler> logger) : IRequestHandler<ResendVerificationEmailCommand, Result>
{
    public async Task<Result> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(emailLower, cancellationToken);

        if (user == null)
        {
            // Don't reveal if user exists
            logger.LogWarning("Resend verification email requested for non-existent email: {Email}", emailLower);
            return Result.Success();
        }

        if (user.EmailVerified)
        {
            logger.LogInformation("Resend verification email requested for already verified user: {Email}", emailLower);
            return Result.Success(); // Already verified, silently succeed
        }

        var newVerificationToken = tokenService.GenerateToken();
        var tokenExpiration = tokenService.GetExpiration();

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var emailVerificationToken = new EmailVerificationToken(
                    Guid.NewGuid(),
                    user.Id,
                    newVerificationToken,
                    tokenExpiration);

                await tokenRepository.AddAsync(emailVerificationToken, ct);
            }, cancellationToken);

            await SendVerificationEmailAsync(user, newVerificationToken, cancellationToken);

            logger.LogInformation("Verification email resent to {Email}", emailLower);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend verification email to {Email}", emailLower);
            return Result.Failure(new Error("INTERNAL_ERROR", "Failed to resend verification email"));
        }
    }

    private async Task SendVerificationEmailAsync(Domain.Users.User user, string verificationToken, CancellationToken cancellationToken)
    {
        var frontendUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(verificationToken);
        var verificationLink = $"{frontendUrl}/verify-email?token={encodedToken}";
        var emailSubject = EmailMessages.Welcome;
        var fullName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : user.Email.Split('@')[0];

        var emailModel = new
        {
            FullName = fullName,
            VerificationLink = verificationLink,
            ExpirationDays = tokenService.GetExpirationDays()
        };

        logger.LogInformation("Preparing to resend verification email to {Email}", user.Email);

        try
        {
            await emailSender.SendEmailWithTemplateAsync(
                user.Email,
                emailSubject,
                "REGISTER",
                emailModel,
                cancellationToken);

            logger.LogInformation("Verification email resent successfully to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification email to {Email}. Error: {ErrorMessage}",
                user.Email, ex.Message);
            throw;
        }
    }
}
