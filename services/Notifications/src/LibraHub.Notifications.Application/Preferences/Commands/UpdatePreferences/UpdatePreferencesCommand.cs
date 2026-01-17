using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Commands.UpdatePreferences;

public record UpdatePreferencesCommand(
    bool EmailEnabled) : IRequest<Result>;
