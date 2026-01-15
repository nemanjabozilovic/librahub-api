using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using LibraHub.Notifications.Domain.Notifications;
using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;

public class GetPreferencesHandler(
    INotificationPreferencesRepository preferencesRepository,
    ICurrentUser currentUser) : IRequestHandler<GetPreferencesQuery, GetPreferencesDto>
{
    public async Task<GetPreferencesDto> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException(NotificationsErrors.User.NotAuthenticated);
        }

        var userId = currentUser.UserId.Value;
        var preferences = await preferencesRepository.GetByUserIdAsync(userId, cancellationToken);

        var allTypes = Enum.GetValues<NotificationType>();
        var preferenceDtos = allTypes.Select(type =>
        {
            var existing = preferences.FirstOrDefault(p => p.Type == type);
            return new PreferenceDto
            {
                Type = type.ToString(),
                EmailEnabled = existing?.EmailEnabled ?? false,
                InAppEnabled = existing?.InAppEnabled ?? false
            };
        }).ToList();

        return new GetPreferencesDto
        {
            Preferences = preferenceDtos
        };
    }
}
