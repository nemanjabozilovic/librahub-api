using LibraHub.BuildingBlocks.Results;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.BuildingBlocks.Abstractions;

public static class CurrentUserExtensions
{
    public static Result<Guid> RequireUserId(this ICurrentUser currentUser, string errorMessage = "User not authenticated")
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result.Failure<Guid>(Error.Unauthorized(errorMessage));
        }

        return Result.Success(currentUser.UserId.Value);
    }
}
