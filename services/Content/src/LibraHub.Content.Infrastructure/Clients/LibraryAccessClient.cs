using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using Microsoft.Extensions.Options;

namespace LibraHub.Content.Infrastructure.Clients;

public class LibraryAccessClient : ILibraryAccessClient
{
    private readonly HttpClient _httpClient;
    private readonly ReadAccessOptions _options;

    public LibraryAccessClient(HttpClient httpClient, IOptions<ReadAccessOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<bool> UserOwnsBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.LibraryApiUrl}/api/users/{userId}/books/{bookId}/owns",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<OwnershipResult>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Owns ?? false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private record OwnershipResult
    {
        public bool Owns { get; init; }
    }
}
