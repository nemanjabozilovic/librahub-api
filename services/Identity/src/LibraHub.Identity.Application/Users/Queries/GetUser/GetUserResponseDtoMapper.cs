using LibraHub.Identity.Domain.Users;

namespace LibraHub.Identity.Application.Users.Queries.GetUser;

public static class GetUserResponseDtoMapper
{
    public static GetUserResponseDto MapFromUser(Domain.Users.User user)
    {
        return new GetUserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Roles = user.Roles.Select(r => r.Role.ToString()).ToList(),
            EmailVerified = user.EmailVerified,
            Status = user.Status.ToString(),
            IsActive = user.Status == UserStatus.Active,
            CreatedAt = new DateTimeOffset(user.CreatedAt, TimeSpan.Zero),
            LastLoginAt = user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value, TimeSpan.Zero) : null
        };
    }
}
