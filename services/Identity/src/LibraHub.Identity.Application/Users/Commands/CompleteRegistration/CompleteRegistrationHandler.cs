using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Commands.CompleteRegistration;

public class CompleteRegistrationHandler(
    IRegistrationCompletionTokenRepository tokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CompleteRegistrationCommand, Result>
{
    public async Task<Result> Handle(CompleteRegistrationCommand request, CancellationToken cancellationToken)
    {
        var token = await tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token == null || !token.IsValid)
        {
            return Result.Failure(Error.Validation("Invalid or expired registration completion token"));
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);
        var shouldVerifyEmail = !user.EmailVerified;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            user.UpdatePassword(passwordHash);
            user.UpdateProfile(request.FirstName, request.LastName, request.Phone, request.DateOfBirth.UtcDateTime);

            if (shouldVerifyEmail)
            {
                user.MarkEmailAsVerified();
            }

            token.MarkAsUsed();

            await userRepository.UpdateAsync(user, ct);
            await tokenRepository.UpdateAsync(token, ct);

            if (shouldVerifyEmail)
            {
                var integrationEvent = new EmailVerifiedV1
                {
                    UserId = user.Id,
                    OccurredAt = clock.UtcNowOffset
                };

                await outboxWriter.WriteAsync(integrationEvent, EventTypes.EmailVerified, ct);
            }
        }, cancellationToken);

        return Result.Success();
    }
}
