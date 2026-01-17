using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUser;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetUsersByIds;

public class GetUsersByIdsQueryHandler(
    IUserRepository userRepository) : IRequestHandler<GetUsersByIdsQuery, Result<GetUsersByIdsResponseDto>>
{
    public async Task<Result<GetUsersByIdsResponseDto>> Handle(GetUsersByIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.UserIds == null || request.UserIds.Count == 0)
        {
            return Result.Failure<GetUsersByIdsResponseDto>(Error.Validation("UserIds list cannot be empty"));
        }

        if (request.UserIds.Count > 100)
        {
            return Result.Failure<GetUsersByIdsResponseDto>(Error.Validation("Maximum 100 user IDs allowed"));
        }

        var users = new List<GetUserResponseDto>();

        foreach (var userId in request.UserIds.Distinct())
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null && user.Status != Domain.Users.UserStatus.Removed)
            {
                users.Add(GetUser.GetUserResponseDtoMapper.MapFromUser(user));
            }
        }

        var response = new GetUsersByIdsResponseDto
        {
            Users = users
        };

        return Result.Success(response);
    }
}
