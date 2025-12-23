using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using LibraHub.Notifications.Domain.Preferences;
using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Commands.UpdatePreferences;

public class UpdatePreferencesHandler(
    INotificationPreferencesRepository preferencesRepository,
    ICurrentUser currentUser) : IRequestHandler<UpdatePreferencesCommand>
{
    public async Task Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(NotificationsErrors.User.NotAuthenticated);
        }

        var userId = currentUser.UserId.Value;
        var preference = await preferencesRepository.GetByUserIdAndTypeAsync(userId, request.Type, cancellationToken);

        if (preference == null)
        {
            preference = new NotificationPreference(
                Guid.NewGuid(),
                userId,
                request.Type,
                request.EmailEnabled,
                request.InAppEnabled);
            await preferencesRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.Update(request.EmailEnabled, request.InAppEnabled);
            await preferencesRepository.UpdateAsync(preference, cancellationToken);
        }
    }
}

