using LibraHub.BuildingBlocks.Http;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraHub.Library.Infrastructure.Clients;

public class CatalogClient : ICatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly LibraryOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CatalogClient> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public CatalogClient(
        HttpClient httpClient,
        IOptions<LibraryOptions> options,
        IMemoryCache cache,
        ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<CatalogBookDetailsDto>> GetBookDetailsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"catalog_book_{bookId}";
        if (_cache.TryGetValue<CatalogBookDetailsDto>(cacheKey, out var cached) && cached != null)
        {
            return Result.Success(cached);
        }

        var url = $"{_options.CatalogApiUrl}/books/{bookId}";
        var result = await _httpClient.GetJsonResultAsync<CatalogBookDetailsDto>(
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

    public async Task<Result<Dictionary<Guid, CatalogBookDetailsDto>>> GetBookDetailsByIdsAsync(
        List<Guid> bookIds,
        CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
        {
            return Result.Success(new Dictionary<Guid, CatalogBookDetailsDto>());
        }

        var distinct = bookIds.Distinct().ToList();
        var tasks = distinct.Select(async id =>
        {
            var r = await GetBookDetailsAsync(id, cancellationToken);
            return (id, r);
        });

        var results = await Task.WhenAll(tasks);

        var dict = new Dictionary<Guid, CatalogBookDetailsDto>();
        var firstFailure = results.Select(x => x.r).FirstOrDefault(x => x.IsFailure);

        foreach (var (id, r) in results)
        {
            if (r.IsSuccess)
            {
                dict[id] = r.Value;
            }
        }

        if (dict.Count == 0 && firstFailure != null)
        {
            return Result.Failure<Dictionary<Guid, CatalogBookDetailsDto>>(firstFailure.Error!);
        }

        return Result.Success(dict);
    }
}

