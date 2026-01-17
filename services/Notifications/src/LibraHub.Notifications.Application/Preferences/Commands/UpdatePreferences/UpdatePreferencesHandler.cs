using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Notifications.Application.Preferences.Commands.UpdatePreferences;

public class UpdatePreferencesHandler(
    IUserNotificationSettingsRepository settingsRepository,
    ICurrentUser currentUser) : IRequestHandler<UpdatePreferencesCommand, Result>
{
    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(NotificationsErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure(userIdResult.Error!);
        }

        var userId = userIdResult.Value;
        var settings = await settingsRepository.GetByUserIdAsync(userId, cancellationToken);

        if (settings == null)
        {
            return Result.Failure(Error.NotFound("User notification settings not found. Please complete registration first."));
        }

        settings.Update(request.EmailEnabled, inAppEnabled: true);
        await settingsRepository.UpsertAsync(settings, cancellationToken);

        return Result.Success();
    }
}
