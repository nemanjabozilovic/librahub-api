using LibraHub.BuildingBlocks.Http;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraHub.Notifications.Infrastructure.Clients;

public class IdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly NotificationsOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdentityClient> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public IdentityClient(
        HttpClient httpClient,
        IOptions<NotificationsOptions> options,
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
}
