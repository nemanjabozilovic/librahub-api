using LibraHub.BuildingBlocks.Results;

namespace LibraHub.Library.Application.Abstractions;

public interface IIdentityClient
{
    Task<Result<UserInfo>> GetUserInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<Guid, UserInfo?>>> GetUsersByIdsAsync(
        List<Guid> userIds,
        CancellationToken cancellationToken = default);
}

public class UserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
}
