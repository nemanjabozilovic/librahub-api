using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Users.Queries.GetUserAvatar;

public record GetUserAvatarQuery(Guid UserId, string FileName) : IRequest<Result<UserAvatarFileDto>>;
