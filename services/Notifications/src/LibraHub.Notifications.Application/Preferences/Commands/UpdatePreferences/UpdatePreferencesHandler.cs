using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Commands.UpdatePreferences;

public class UpdatePreferencesHandler(
    IUserNotificationSettingsRepository settingsRepository,
    ICurrentUser currentUser) : IRequestHandler<UpdatePreferencesCommand>
{
    public async Task Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(NotificationsErrors.User.NotAuthenticated);
        }

        var userId = currentUser.UserId.Value;
        var settings = await settingsRepository.GetByUserIdAsync(userId, cancellationToken);

        if (settings == null)
        {
            throw new InvalidOperationException("User notification settings not found. Please complete registration first.");
        }

        settings.Update(request.EmailEnabled, inAppEnabled: true);
        await settingsRepository.UpsertAsync(settings, cancellationToken);
    }
}
