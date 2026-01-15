using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.BuildingBlocks.Http;
using LibraHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<CatalogReadClient> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public CatalogReadClient(
        HttpClient httpClient,
        IOptions<ReadAccessOptions> options,
        IMemoryCache cache,
        ILogger<CatalogReadClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Result<BookInfo>> GetBookInfoAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"book_info_{bookId}";

        if (_cache.TryGetValue<BookInfo>(cacheKey, out var cachedInfo) && cachedInfo != null)
        {
            return Result.Success(cachedInfo);
        }

        var url = $"{_options.CatalogApiUrl}/books/{bookId}/info";
        var result = await _httpClient.GetJsonResultAsync<BookInfo>(
            url,
            _logger,
            notFoundResourceName: "Book",
            cancellationToken);

        if (result.IsSuccess)
        {
            _cache.Set(cacheKey, result.Value, CacheExpiration);
        }

        return result;
    }
}
