namespace LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;

public record GetPreferencesDto
{
    public List<PreferenceDto> Preferences { get; init; } = new();
}

public record PreferenceDto
{
    public string Type { get; init; } = string.Empty;
    public bool EmailEnabled { get; init; }
    public bool InAppEnabled { get; init; }
}

