using LibraHub.Notifications.Application.Abstractions;
using LibraHub.Notifications.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Notifications.Infrastructure.Clients;

public class IdentityClient : IIdentityClient
{
    private readonly HttpClient _httpClient;
    private readonly NotificationsOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdentityClient(HttpClient httpClient, IOptions<NotificationsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<UserInfo?> GetUserInfoAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
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

            return userInfo;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}

