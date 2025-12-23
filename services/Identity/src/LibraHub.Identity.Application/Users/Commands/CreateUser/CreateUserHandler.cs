using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Tokens;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Commands.CreateUser;

public class CreateUserHandler(
    IUserRepository userRepository,
    IRegistrationCompletionTokenRepository tokenRepository,
    IRegistrationCompletionTokenService tokenService,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<CreateUserHandler> logger) : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(emailLower, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Email already exists"));
        }

        // Generate temporary password
        var tempPassword = GenerateTemporaryPassword();
        var passwordHash = passwordHasher.HashPassword(tempPassword);

        // Create user with minimal data
        var user = new User(
            Guid.NewGuid(),
            emailLower,
            passwordHash,
            string.Empty, // FirstName - will be set during completion
            string.Empty, // LastName - will be set during completion
            null, // Phone
            null); // DateOfBirth

        // Remove default User role and add requested role
        user.RemoveRole(Role.User);
        user.AddRole(request.Role);

        await userRepository.AddAsync(user, cancellationToken);

        // Generate registration completion token
        var completionToken = tokenService.GenerateToken();
        var tokenExpiration = tokenService.GetExpiration();
        var registrationToken = new RegistrationCompletionToken(
            Guid.NewGuid(),
            user.Id,
            completionToken,
            tokenExpiration);

        await tokenRepository.AddAsync(registrationToken, cancellationToken);

        // Send email with completion link
        var frontendUrl = configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl configuration is required");
        var completionLink = $"{frontendUrl}/complete-registration?token={completionToken}";
        var emailSubject = "Complete Your LibraHub Registration";
        var emailModel = new
        {
            Email = user.Email,
            CompletionLink = completionLink,
            ExpirationHours = tokenService.GetExpirationHours()
        };

        try
        {
            await emailSender.SendEmailWithTemplateAsync(
                user.Email,
                emailSubject,
                "COMPLETE_REGISTRATION",
                emailModel,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send registration completion email to {Email}", user.Email);
            // Don't fail user creation if email sending fails
        }

        return Result.Success(user.Id);
    }

    private static string GenerateTemporaryPassword()
    {
        // Generate a secure temporary password
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 16)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

