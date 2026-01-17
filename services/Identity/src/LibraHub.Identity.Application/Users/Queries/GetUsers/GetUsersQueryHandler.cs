using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler(
    IUserRepository userRepository) : IRequestHandler<GetUsersQuery, Result<GetUsersResponseDto>>
{
    public async Task<Result<GetUsersResponseDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (request.Skip < 0)
        {
            return Result.Failure<GetUsersResponseDto>(Error.Validation("Skip must be greater than or equal to 0"));
        }

        if (request.Take < 1 || request.Take > 100)
        {
            return Result.Failure<GetUsersResponseDto>(Error.Validation("Take must be between 1 and 100"));
        }

        var users = await userRepository.GetUsersPagedAsync(request.Skip, request.Take, cancellationToken);
        var totalCount = await userRepository.CountAllAsync(cancellationToken);

        var userDtos = users.Select(UserDtoMapper.MapFromUser).ToList();

        var response = new GetUsersResponseDto
        {
            Users = userDtos,
            TotalCount = totalCount
        };

        return Result.Success(response);
    }
}
