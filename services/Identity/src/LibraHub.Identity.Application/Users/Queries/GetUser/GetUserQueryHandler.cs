using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetUser;

public class GetUserQueryHandler(
    IUserRepository userRepository) : IRequestHandler<GetUserQuery, Result<GetUserResponseDto>>
{
    public async Task<Result<GetUserResponseDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null || user.Status == Domain.Users.UserStatus.Removed)
        {
            return Result.Failure<GetUserResponseDto>(Error.NotFound("User not found"));
        }

        var response = GetUserResponseDtoMapper.MapFromUser(user);

        return Result.Success(response);
    }
}
