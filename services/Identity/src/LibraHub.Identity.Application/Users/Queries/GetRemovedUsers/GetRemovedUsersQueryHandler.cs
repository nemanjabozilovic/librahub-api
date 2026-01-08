using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUsers;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetRemovedUsers;

public class GetRemovedUsersQueryHandler : IRequestHandler<GetRemovedUsersQuery, Result<GetRemovedUsersResponseDto>>
{
    private readonly IUserRepository _userRepository;

    public GetRemovedUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

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

        var users = await _userRepository.GetRemovedUsersPagedAsync(request.Skip, request.Take, cancellationToken);
        var totalCount = await _userRepository.CountRemovedAsync(cancellationToken);

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
            CreatedAt = new DateTimeOffset(user.CreatedAt, TimeSpan.Zero),
            LastLoginAt = user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value, TimeSpan.Zero) : null
        }).ToList();

        var response = new GetRemovedUsersResponseDto
        {
            Users = userDtos,
            TotalCount = totalCount
        };

        return Result.Success(response);
    }
}

