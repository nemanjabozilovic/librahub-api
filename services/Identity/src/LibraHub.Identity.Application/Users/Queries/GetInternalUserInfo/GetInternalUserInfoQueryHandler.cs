using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Queries.GetInternalUserInfo;

public class GetInternalUserInfoQueryHandler(
    IUserRepository userRepository,
    ILogger<GetInternalUserInfoQueryHandler> logger)
    : IRequestHandler<GetInternalUserInfoQuery, Result<InternalUserInfoDto>>
{
    public async Task<Result<InternalUserInfoDto>> Handle(GetInternalUserInfoQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.Status == UserStatus.Removed)
        {
            logger.LogWarning("Internal user info not found for UserId: {UserId}", request.UserId);
            return Result.Failure<InternalUserInfoDto>(Error.NotFound("User"));
        }

        return Result.Success(new InternalUserInfoDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            FullName = user.DisplayName,
            IsActive = user.Status == UserStatus.Active,
            IsEmailVerified = user.EmailVerified
        });
    }
}
