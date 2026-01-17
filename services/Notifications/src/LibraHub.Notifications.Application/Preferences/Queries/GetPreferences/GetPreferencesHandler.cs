using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Domain.Errors;
using MediatR;

namespace LibraHub.Notifications.Application.Preferences.Queries.GetPreferences;

public class GetPreferencesHandler(
    IUserNotificationSettingsRepository settingsRepository,
    ICurrentUser currentUser) : IRequestHandler<GetPreferencesQuery, Result<GetPreferencesDto>>
{
    public async Task<Result<GetPreferencesDto>> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(NotificationsErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure<GetPreferencesDto>(userIdResult.Error!);
        }

        var userId = userIdResult.Value;
        var settings = await settingsRepository.GetByUserIdAsync(userId, cancellationToken);

        return Result.Success(new GetPreferencesDto
        {
            EmailEnabled = settings?.EmailEnabled ?? false
        });
    }
}
