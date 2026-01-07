using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Application.Users.Queries.GetUser;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetUsersByIds;

public class GetUsersByIdsQueryHandler : IRequestHandler<GetUsersByIdsQuery, Result<GetUsersByIdsResponseDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByIdsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

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
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                users.Add(new GetUserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DisplayName = user.DisplayName,
                    Roles = user.Roles.Select(r => r.Role.ToString()).ToList(),
                    EmailVerified = user.EmailVerified,
                    Status = user.Status.ToString(),
                    IsActive = user.Status == Domain.Users.UserStatus.Active,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }
        }

        var response = new GetUsersByIdsResponseDto
        {
            Users = users
        };

        return Result.Success(response);
    }
}
