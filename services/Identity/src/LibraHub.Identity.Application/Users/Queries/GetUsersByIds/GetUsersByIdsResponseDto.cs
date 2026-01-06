using LibraHub.Identity.Application.Users.Queries.GetUser;

namespace LibraHub.Identity.Application.Users.Queries.GetUsersByIds;

public record GetUsersByIdsResponseDto
{
    public List<GetUserResponseDto> Users { get; init; } = new();
}

