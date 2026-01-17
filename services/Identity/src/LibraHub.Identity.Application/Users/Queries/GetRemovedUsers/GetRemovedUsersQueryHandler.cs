using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUsers;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetRemovedUsers;

public class GetRemovedUsersQueryHandler(
    IUserRepository userRepository) : IRequestHandler<GetRemovedUsersQuery, Result<GetRemovedUsersResponseDto>>
{
    public async Task<Result<GetRemovedUsersResponseDto>> Handle(GetRemovedUsersQuery request, CancellationToken cancellationToken)
    {
        if (request.Skip < 0)
        {
            return Result.Failure<GetRemovedUsersResponseDto>(Error.Validation("Skip must be greater than or equal to 0"));
        }

        if (request.Take < 1 || request.Take > 100)
        {
            return Result.Failure<GetRemovedUsersResponseDto>(Error.Validation("Take must be between 1 and 100"));
        }

        var users = await userRepository.GetRemovedUsersPagedAsync(request.Skip, request.Take, cancellationToken);
        var totalCount = await userRepository.CountRemovedAsync(cancellationToken);

        var userDtos = users.Select(UserDtoMapper.MapFromUser).ToList();

        var response = new GetRemovedUsersResponseDto
        {
            Users = userDtos,
            TotalCount = totalCount
        };

        return Result.Success(response);
    }
}
