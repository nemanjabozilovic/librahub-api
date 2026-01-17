using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUsers;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Commands.UpdateUser;

public class UpdateUserHandler(
    IUserRepository userRepository) : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<UserDto>(Error.NotFound("User not found"));
        }

        user.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.DateOfBirth);

        if (request.EmailVerified.HasValue && request.EmailVerified.Value && !user.EmailVerified)
        {
            user.MarkEmailAsVerified();
        }

        await userRepository.UpdateAsync(user, cancellationToken);

        var userDto = UserDtoMapper.MapFromUser(user);

        return Result.Success(userDto);
    }
}
