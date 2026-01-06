namespace LibraHub.Library.Application.Abstractions;

public interface IIdentityClient
{
    Task<UserInfo?> GetUserInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, UserInfo?>> GetUsersByIdsAsync(
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

