namespace LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;

public record GetPreferencesDto
{
    public bool EmailEnabled { get; init; }
}
