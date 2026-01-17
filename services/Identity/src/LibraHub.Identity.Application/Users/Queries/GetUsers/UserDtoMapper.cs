namespace LibraHub.Identity.Application.Users.Queries.GetUsers;

public static class UserDtoMapper
{
    public static UserDto MapFromUser(Domain.Users.User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Roles = user.Roles.Select(r => r.Role.ToString()).ToList(),
            EmailVerified = user.EmailVerified,
            Status = user.Status.ToString(),
            CreatedAt = new DateTimeOffset(user.CreatedAt, TimeSpan.Zero),
            LastLoginAt = user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value, TimeSpan.Zero) : null
        };
    }
}
