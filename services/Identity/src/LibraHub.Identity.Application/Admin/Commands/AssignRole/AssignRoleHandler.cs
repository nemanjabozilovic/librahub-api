using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Admin.Commands.AssignRole;

public class AssignRoleHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IClock _clock;

    public AssignRoleHandler(
        IUserRepository userRepository,
        IOutboxWriter outboxWriter,
        IClock clock)
    {
        _userRepository = userRepository;
        _outboxWriter = outboxWriter;
        _clock = clock;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        if (request.Role == Role.Admin && !request.Assign)
        {
            var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
            if (adminCount <= 1 && user.IsAdmin())
            {
                return Result.Failure(Error.Validation("Cannot remove the last admin user"));
            }
        }

        if (request.Assign)
        {
            user.AddRole(request.Role);
        }
        else
        {
            user.RemoveRole(request.Role);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        var integrationEvent = new RoleAssignedV1
        {
            UserId = user.Id,
            Role = request.Role.ToString(),
            OccurredAt = _clock.UtcNowOffset
        };

        await _outboxWriter.WriteAsync(integrationEvent, EventTypes.RoleAssigned, cancellationToken);

        var settingsEvent = new UserNotificationSettingsChangedV1
        {
            UserId = user.Id,
            Email = user.Email,
            IsActive = user.Status == UserStatus.Active,
            IsStaff = user.IsStaff(),
            EmailAnnouncementsEnabled = user.EmailAnnouncementsEnabled,
            EmailPromotionsEnabled = user.EmailPromotionsEnabled,
            OccurredAt = _clock.UtcNowOffset
        };

        await _outboxWriter.WriteAsync(settingsEvent, EventTypes.UserNotificationSettingsChanged, cancellationToken);

        return Result.Success();
    }
}
