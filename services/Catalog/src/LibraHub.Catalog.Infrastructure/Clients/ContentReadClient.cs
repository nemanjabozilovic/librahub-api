using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace LibraHub.Catalog.Infrastructure.Clients;

public class ContentReadClient : IContentReadClient
{
    private readonly HttpClient _httpClient;
    private readonly CatalogOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public ContentReadClient(HttpClient httpClient, IOptions<CatalogOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<BookEditionInfoDto>> GetBookEditionsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.ContentApiUrl}/api/books/{bookId}/editions";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<BookEditionInfoDto>();
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var editions = JsonSerializer.Deserialize<List<BookEditionInfoDto>>(content, _jsonOptions)
                ?? new List<BookEditionInfoDto>();

            return editions;
        }
        catch (HttpRequestException)
        {
            return new List<BookEditionInfoDto>();
        }
    }

    public async Task<Dictionary<Guid, List<BookEditionInfoDto>>> GetBookEditionsBatchAsync(List<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
        {
            return new Dictionary<Guid, List<BookEditionInfoDto>>();
        }

        try
        {
            var url = $"{_options.ContentApiUrl}/api/books/editions/batch";
            var requestBody = new { BookIds = bookIds };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<Dictionary<Guid, List<BookEditionInfoDto>>>(responseContent, _jsonOptions)
                ?? new Dictionary<Guid, List<BookEditionInfoDto>>();

            return result;
        }
        catch (HttpRequestException)
        {
            return bookIds.ToDictionary(id => id, _ => new List<BookEditionInfoDto>());
        }
    }
}
