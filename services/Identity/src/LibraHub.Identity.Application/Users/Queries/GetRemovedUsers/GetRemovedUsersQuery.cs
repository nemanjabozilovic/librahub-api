using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Queries.GetRemovedUsers;

public record GetRemovedUsersQuery(int Skip = 0, int Take = 50) : IRequest<Result<GetRemovedUsersResponseDto>>;
