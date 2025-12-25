using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Queries.GetUsers;

public record GetUsersQuery(int Skip = 0, int Take = 50) : IRequest<Result<GetUsersResponseDto>>;

