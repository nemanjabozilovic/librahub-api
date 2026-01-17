using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.VerifyEmail;

public class VerifyEmailHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository tokenRepository,
    IOutboxWriter outboxWriter,
    IClock clock) : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var token = await tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token == null || !token.IsValid)
        {
            return Result.Failure(Error.Validation("Invalid or expired verification token"));
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        if (user.EmailVerified)
        {
            return Result.Success();
        }

        user.MarkEmailAsVerified();
        token.MarkAsUsed();

        await userRepository.UpdateAsync(user, cancellationToken);
        await tokenRepository.UpdateAsync(token, cancellationToken);

        var integrationEvent = new EmailVerifiedV1
        {
            UserId = user.Id,
            OccurredAt = clock.UtcNowOffset
        };

        await outboxWriter.WriteAsync(integrationEvent, EventTypes.EmailVerified, cancellationToken);

        return Result.Success();
    }
}
