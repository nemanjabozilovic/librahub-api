using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Orders.Infrastructure.Clients;

public class IdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly OrdersOptions _options;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public IdentityClient(
        HttpClient httpClient,
        IOptions<OrdersOptions> options,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<UserInfo?> GetUserInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user_info_{userId}";

        if (_cache.TryGetValue<UserInfo>(cacheKey, out var cachedInfo))
        {
            return cachedInfo;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.IdentityApiUrl}/api/users/{userId}/info",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var userInfo = JsonSerializer.Deserialize<UserInfo>(content, _jsonOptions);

            if (userInfo != null)
            {
                _cache.Set(cacheKey, userInfo, CacheExpiration);
            }

            return userInfo;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<Dictionary<Guid, UserInfo?>> GetUsersByIdsAsync(
        List<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds == null || userIds.Count == 0)
        {
            return new Dictionary<Guid, UserInfo?>();
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
            return result;
        }

        try
        {
            var requestBody = System.Text.Json.JsonSerializer.Serialize(new { UserIds = uncachedIds });
            var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.IdentityApiUrl}/api/users/by-ids",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var usersResponse = System.Text.Json.JsonSerializer.Deserialize<UsersByIdsResponse>(
                responseContent,
                _jsonOptions);

            if (usersResponse != null)
            {
                foreach (var userInfo in usersResponse.Users)
                {
                    result[userInfo.Id] = userInfo;
                    var cacheKey = $"user_info_{userInfo.Id}";
                    _cache.Set(cacheKey, userInfo, CacheExpiration);
                }
            }

            foreach (var userId in uncachedIds)
            {
                if (!result.ContainsKey(userId))
                {
                    result[userId] = null;
                }
            }
        }
        catch (HttpRequestException)
        {
        }

        return result;
    }

    private class UsersByIdsResponse
    {
        public List<UserInfo> Users { get; set; } = new();
    }
}
