using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Queries.GetInternalUserInfo;

public record GetInternalUserInfoQuery(Guid UserId) : IRequest<Result<InternalUserInfoDto>>;
