using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Identity.Application.Me.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<Result<UserProfileDto>>;
