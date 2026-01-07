using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Queries.GetUsersByIds;

public record GetUsersByIdsQuery(List<Guid> UserIds) : IRequest<Result<GetUsersByIdsResponseDto>>;
