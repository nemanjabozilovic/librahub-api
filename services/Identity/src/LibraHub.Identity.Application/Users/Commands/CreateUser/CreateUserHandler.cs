using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Constants;
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
    IEmailSender emailSender,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    ILogger<CreateUserHandler> logger) : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(emailLower, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Email already exists"));
        }

        var user = CreateUser(emailLower, request.Role);
        var completionToken = await SaveUserWithTokenAsync(user, cancellationToken);

        await SendCompletionEmailAsync(user, completionToken, cancellationToken);

        return Result.Success(user.Id);
    }

    private User CreateUser(string email, Role role)
    {
        var user = new User(
            Guid.NewGuid(),
            email,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            default);

        user.AddRole(role);

        return user;
    }

    private async Task<string> SaveUserWithTokenAsync(User user, CancellationToken cancellationToken)
    {
        var completionToken = tokenService.GenerateToken();
        var tokenExpiration = tokenService.GetExpiration();

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await userRepository.AddAsync(user, ct);

                var registrationToken = new RegistrationCompletionToken(
                    Guid.NewGuid(),
                    user.Id,
                    completionToken,
                    tokenExpiration);

                await tokenRepository.AddAsync(registrationToken, ct);
            }, cancellationToken);

            return completionToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create user with email: {Email}", user.Email);
            throw;
        }
    }

    private async Task SendCompletionEmailAsync(User user, string completionToken, CancellationToken cancellationToken)
    {
        var frontendUrl = configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl configuration is required");
        var encodedToken = Uri.EscapeDataString(completionToken);
        var encodedEmail = Uri.EscapeDataString(user.Email);
        var completionLink = $"{frontendUrl.TrimEnd('/')}/complete-registration?token={encodedToken}&email={encodedEmail}";
        var emailSubject = EmailMessages.CompleteRegistration;
        var fullName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : user.Email.Split('@')[0];

        var emailModel = new
        {
            FullName = fullName,
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
        }
    }
}

