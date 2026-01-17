using LibraHub.Identity.Application.Users.Queries.GetUsers;

namespace LibraHub.Identity.Application.Users.Queries.GetRemovedUsers;

public record GetRemovedUsersResponseDto
{
    public List<UserDto> Users { get; init; } = new();
    public int TotalCount { get; init; }
}
