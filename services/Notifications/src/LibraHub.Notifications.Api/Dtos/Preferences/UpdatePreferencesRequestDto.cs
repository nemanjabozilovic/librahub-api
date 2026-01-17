namespace LibraHub.Notifications.Api.Dtos.Preferences;

public record UpdatePreferencesRequestDto
{
    public bool EmailEnabled { get; init; }
}
