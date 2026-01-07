using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Constants;
using LibraHub.Identity.Application.Options;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.Register;

public class RegisterHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository tokenRepository,
    IPasswordHasher passwordHasher,
    IEmailVerificationTokenService tokenService,
    IOutboxWriter outboxWriter,
    IEmailSender emailSender,
    IClock clock,
    IUnitOfWork unitOfWork,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<RegisterHandler> logger) : IRequestHandler<RegisterCommand, Result>
{
    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(emailLower, cancellationToken))
        {
            return Result.Failure(Error.Conflict("Email already exists"));
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);
        var user = CreateUser(emailLower, request, passwordHash);
        var verificationToken = await SaveUserWithTokenAsync(user, cancellationToken);

        await SendWelcomeEmailAsync(user, verificationToken, cancellationToken);

        return Result.Success();
    }

    private static User CreateUser(string emailLower, RegisterCommand request, string passwordHash)
    {
        var user = new User(
            Guid.NewGuid(),
            emailLower,
            passwordHash,
            request.FirstName ?? string.Empty,
            request.LastName ?? string.Empty,
            request.Phone,
            request.DateOfBirth.UtcDateTime);

        user.AddRole(Role.User);
        return user;
    }

    private async Task<string> SaveUserWithTokenAsync(User user, CancellationToken cancellationToken)
    {
        var verificationToken = tokenService.GenerateToken();
        var tokenExpiration = tokenService.GetExpiration();

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await userRepository.AddAsync(user, ct);

                var emailVerificationToken = new Domain.Tokens.EmailVerificationToken(
                    Guid.NewGuid(),
                    user.Id,
                    verificationToken,
                    tokenExpiration);

                await tokenRepository.AddAsync(emailVerificationToken, ct);

                var integrationEvent = new UserRegisteredV1
                {
                    UserId = user.Id,
                    Email = user.Email,
                    OccurredAt = clock.UtcNowOffset
                };

                await outboxWriter.WriteAsync(integrationEvent, EventTypes.UserRegistered, ct);
            }, cancellationToken);

            return verificationToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register user with email: {Email}", user.Email);
            throw;
        }
    }

    private async Task SendWelcomeEmailAsync(User user, string verificationToken, CancellationToken cancellationToken)
    {
        var frontendUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(verificationToken);
        var verificationLink = $"{frontendUrl}/verify-email?token={encodedToken}";
        var emailSubject = EmailMessages.Welcome;
        var emailModel = new
        {
            FullName = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.Email,
            VerificationLink = verificationLink,
            ExpirationDays = tokenService.GetExpirationDays()
        };

        logger.LogInformation("Preparing to send welcome email to {Email} with verification link", user.Email);

        try
        {
            await emailSender.SendEmailWithTemplateAsync(
                user.Email,
                emailSubject,
                "REGISTER",
                emailModel,
                cancellationToken);

            logger.LogInformation("Welcome email sent successfully to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}. Error: {ErrorMessage}",
                user.Email, ex.Message);
        }
    }
}
