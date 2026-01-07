using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Notifications.Infrastructure.Clients;

public class IdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly NotificationsOptions _options;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public IdentityClient(
        HttpClient httpClient,
        IOptions<NotificationsOptions> options,
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
}
