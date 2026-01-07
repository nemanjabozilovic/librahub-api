using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;

public record GetPreferencesQuery : IRequest<GetPreferencesDto>;
