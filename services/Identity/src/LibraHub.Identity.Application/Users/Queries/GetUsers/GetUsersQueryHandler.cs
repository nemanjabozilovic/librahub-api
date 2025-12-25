using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<GetUsersResponseDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

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

        var users = await _userRepository.GetUsersPagedAsync(request.Skip, request.Take, cancellationToken);
        var totalCount = await _userRepository.CountAllAsync(cancellationToken);

        var userDtos = users.Select(user => new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Roles = user.Roles.Select(r => r.Role.ToString()).ToList(),
            EmailVerified = user.EmailVerified,
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        }).ToList();

        var response = new GetUsersResponseDto
        {
            Users = userDtos,
            TotalCount = totalCount
        };

        return Result.Success(response);
    }
}

