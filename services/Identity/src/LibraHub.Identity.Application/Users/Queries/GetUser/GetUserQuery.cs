using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Queries.GetUser;

public record GetUserQuery(Guid UserId) : IRequest<Result<GetUserResponseDto>>;
