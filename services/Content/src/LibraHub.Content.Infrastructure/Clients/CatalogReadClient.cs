using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Content.Infrastructure.Clients;

public class CatalogReadClient : ICatalogReadClient
{
    private readonly HttpClient _httpClient;
    private readonly ReadAccessOptions _options;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public CatalogReadClient(
        HttpClient httpClient,
        IOptions<ReadAccessOptions> options,
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

    public async Task<BookInfo?> GetBookInfoAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"book_info_{bookId}";

        if (_cache.TryGetValue<BookInfo>(cacheKey, out var cachedInfo))
        {
            return cachedInfo;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.CatalogApiUrl}/books/{bookId}/info",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var bookInfo = JsonSerializer.Deserialize<BookInfo>(content, _jsonOptions);

            if (bookInfo != null)
            {
                _cache.Set(cacheKey, bookInfo, CacheExpiration);
            }

            return bookInfo;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
