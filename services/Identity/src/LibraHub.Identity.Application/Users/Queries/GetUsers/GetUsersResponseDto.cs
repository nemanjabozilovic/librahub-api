namespace LibraHub.Identity.Application.Users.Queries.GetUsers;

public record GetUsersResponseDto
{
    public List<UserDto> Users { get; init; } = new();
    public int TotalCount { get; init; }
}

