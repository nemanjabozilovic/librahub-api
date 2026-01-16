using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;

public class GetPreferencesHandler(
    IUserNotificationSettingsRepository settingsRepository,
    ICurrentUser currentUser) : IRequestHandler<GetPreferencesQuery, GetPreferencesDto>
{
    public async Task<GetPreferencesDto> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(NotificationsErrors.User.NotAuthenticated);
        }

        var userId = currentUser.UserId.Value;
        var settings = await settingsRepository.GetByUserIdAsync(userId, cancellationToken);

        return new GetPreferencesDto
        {
            EmailEnabled = settings?.EmailEnabled ?? false
        };
    }
}
