namespace LibraHub.Notifications.Api.Dtos.Preferences;

public record UpdatePreferencesRequestDto
{
    public string Type { get; init; } = string.Empty;
    public bool EmailEnabled { get; init; }
    public bool InAppEnabled { get; init; }
}


