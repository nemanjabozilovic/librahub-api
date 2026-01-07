using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Admin.Commands.DisableUser;

public class DisableUserHandler : IRequestHandler<DisableUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IClock _clock;

    public DisableUserHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOutboxWriter outboxWriter,
        IClock clock)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _outboxWriter = outboxWriter;
        _clock = clock;
    }

    public async Task<Result> Handle(DisableUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        if (request.Disable)
        {
            // Prevent disabling the last admin
            if (user.IsAdmin())
            {
                var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
                if (adminCount <= 1)
                {
                    return Result.Failure(Error.Validation("Cannot disable the last admin user"));
                }
            }

            user.Disable(request.Reason);
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

            // Publish integration event
            var integrationEvent = new UserDisabledV1
            {
                UserId = user.Id,
                Reason = request.Reason,
                OccurredAt = _clock.UtcNowOffset
            };

            await _outboxWriter.WriteAsync(integrationEvent, EventTypes.UserDisabled, cancellationToken);
        }
        else
        {
            user.Enable();
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return Result.Success();
    }
}
