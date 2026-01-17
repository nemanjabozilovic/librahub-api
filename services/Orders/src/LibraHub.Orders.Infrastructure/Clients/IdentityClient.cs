using LibraHub.BuildingBlocks.Http;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Orders.Infrastructure.Clients;

public class IdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly OrdersOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdentityClient> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public IdentityClient(
        HttpClient httpClient,
        IOptions<OrdersOptions> options,
        IMemoryCache cache,
        ILogger<IdentityClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<UserInfo>> GetUserInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user_info_{userId}";

        if (_cache.TryGetValue<UserInfo>(cacheKey, out var cachedInfo) && cachedInfo != null)
        {
            return Result.Success(cachedInfo);
        }

        var url = $"{_options.IdentityApiUrl}/internal/users/{userId}/info";
        var result = await _httpClient.GetJsonResultAsync<UserInfo>(
            url,
            _logger,
            notFoundResourceName: "User",
            cancellationToken);

        if (result.IsSuccess)
        {
            _cache.Set(cacheKey, result.Value, CacheExpiration);
        }

        return result;
    }

    public async Task<Result<Dictionary<Guid, UserInfo?>>> GetUsersByIdsAsync(
        List<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds == null || userIds.Count == 0)
        {
            return Result.Success(new Dictionary<Guid, UserInfo?>());
        }

        var result = new Dictionary<Guid, UserInfo?>();
        var uncachedIds = new List<Guid>();

        foreach (var userId in userIds.Distinct())
        {
            var cacheKey = $"user_info_{userId}";
            if (_cache.TryGetValue<UserInfo>(cacheKey, out var cachedInfo))
            {
                result[userId] = cachedInfo;
            }
            else
            {
                uncachedIds.Add(userId);
            }
        }

        if (uncachedIds.Count == 0)
        {
            return Result.Success(result);
        }

        var requestBody = JsonSerializer.Serialize(new { UserIds = uncachedIds });
        var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

        var url = $"{_options.IdentityApiUrl}/users/by-ids";
        var responseResult = await _httpClient.PostJsonResultAsync<UsersByIdsResponse>(
            url,
            content,
            _logger,
            notFoundResourceName: "Users",
            cancellationToken);

        if (responseResult.IsFailure)
        {
            return Result.Failure<Dictionary<Guid, UserInfo?>>(
                responseResult.Error ?? Error.Unexpected("Downstream call failed"));
        }

        foreach (var userInfo in responseResult.Value.Users)
        {
            result[userInfo.Id] = userInfo;
            var cacheKey = $"user_info_{userInfo.Id}";
            _cache.Set(cacheKey, userInfo, CacheExpiration);
        }

        foreach (var userId in uncachedIds)
        {
            if (!result.ContainsKey(userId))
            {
                result[userId] = null;
            }
        }

        return Result.Success(result);
    }

    private class UsersByIdsResponse
    {
        public List<UserInfo> Users { get; set; } = new();
    }
}
