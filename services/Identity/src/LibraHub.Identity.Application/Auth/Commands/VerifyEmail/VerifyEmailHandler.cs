using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Auth.Commands.VerifyEmail;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IClock _clock;

    public VerifyEmailHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IOutboxWriter outboxWriter,
        IClock clock)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _outboxWriter = outboxWriter;
        _clock = clock;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var token = await _tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token == null || !token.IsValid)
        {
            return Result.Failure(Error.Validation("Invalid or expired verification token"));
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
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

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _tokenRepository.UpdateAsync(token, cancellationToken);

        var integrationEvent = new EmailVerifiedV1
        {
            UserId = user.Id,
            OccurredAt = _clock.UtcNowOffset
        };

        await _outboxWriter.WriteAsync(integrationEvent, EventTypes.EmailVerified, cancellationToken);

        return Result.Success();
    }
}
