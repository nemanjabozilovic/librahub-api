using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetUser;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<GetUserResponseDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetUserResponseDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null || user.Status == Domain.Users.UserStatus.Removed)
        {
            return Result.Failure<GetUserResponseDto>(Error.NotFound("User not found"));
        }

        var response = new GetUserResponseDto
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
            CreatedAt = new DateTimeOffset(user.CreatedAt, TimeSpan.Zero),
            LastLoginAt = user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value, TimeSpan.Zero) : null
        };

        return Result.Success(response);
    }
}
