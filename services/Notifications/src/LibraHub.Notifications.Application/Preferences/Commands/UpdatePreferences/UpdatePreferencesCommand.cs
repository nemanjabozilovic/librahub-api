using LibraHub.Notifications.Domain.Notifications;
using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Commands.UpdatePreferences;

public record UpdatePreferencesCommand(
    NotificationType Type,
    bool EmailEnabled,
    bool InAppEnabled) : IRequest;

