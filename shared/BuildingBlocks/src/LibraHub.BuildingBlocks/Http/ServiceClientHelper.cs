using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace LibraHub.BuildingBlocks.Http;

public class ServiceClientHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceClientHelper> _logger;

    public ServiceClientHelper(HttpClient httpClient, ILogger<ServiceClientHelper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(
        string baseUrl,
        string endpoint,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}");

            if (!string.IsNullOrWhiteSpace(authorizationToken))
            {
                request.Headers.Add("Authorization", authorizationToken);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from {BaseUrl}/{Endpoint}", baseUrl, endpoint);
            return null;
        }
    }
}
