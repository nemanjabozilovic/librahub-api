using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Orders.Infrastructure.Clients;

public class LibraryOwnershipClient : ILibraryOwnershipClient
{
    private readonly HttpClient _httpClient;
    private readonly OrdersOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public LibraryOwnershipClient(HttpClient httpClient, IOptions<OrdersOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<bool> UserOwnsBookAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken = default)
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
            var result = JsonSerializer.Deserialize<OwnershipResult>(content, _jsonOptions);

            return result?.Owns ?? false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<List<Guid>> GetOwnedBookIdsAsync(
        Guid userId,
        List<Guid> bookIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                BookIds = bookIds
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.LibraryApiUrl}/api/users/{userId}/books/check-ownership",
                content,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<Guid>();
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OwnershipCheckResult>(responseContent, _jsonOptions);

            return result?.OwnedBookIds ?? new List<Guid>();
        }
        catch (HttpRequestException)
        {
            return new List<Guid>();
        }
    }

    private record OwnershipResult
    {
        public bool Owns { get; init; }
    }

    private record OwnershipCheckResult
    {
        public List<Guid> OwnedBookIds { get; init; } = new();
    }
}
