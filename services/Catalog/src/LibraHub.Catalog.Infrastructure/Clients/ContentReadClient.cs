using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace LibraHub.Catalog.Infrastructure.Clients;

public class ContentReadClient : IContentReadClient
{
    private readonly HttpClient _httpClient;
    private readonly CatalogOptions _options;
    private readonly ILogger<ContentReadClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ContentReadClient(HttpClient httpClient, IOptions<CatalogOptions> options, ILogger<ContentReadClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string?> GetBookCoverRefAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.ContentApiUrl}/api/books/{bookId}/cover";
            _logger.LogDebug("Calling Content API for cover ref: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("No cover found for book {BookId}", bookId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var coverDto = JsonSerializer.Deserialize<BookCoverDto>(content, _jsonOptions);

            _logger.LogDebug("Retrieved cover ref for book {BookId}: {CoverRef}", bookId, coverDto?.CoverRef);
            return coverDto?.CoverRef;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Content API for book {BookId} cover. URL: {Url}", bookId, $"{_options.ContentApiUrl}/api/books/{bookId}/cover");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting cover for book {BookId}", bookId);
            return null;
        }
    }

    public async Task<List<BookEditionInfoDto>> GetBookEditionsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.ContentApiUrl}/api/books/{bookId}/editions";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("No editions found for book {BookId}", bookId);
                return new List<BookEditionInfoDto>();
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var editions = JsonSerializer.Deserialize<List<BookEditionInfoDto>>(content, _jsonOptions)
                ?? new List<BookEditionInfoDto>();

            _logger.LogDebug("Retrieved {Count} editions for book {BookId}", editions.Count, bookId);
            return editions;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Content API for book {BookId} editions. URL: {Url}", bookId, $"{_options.ContentApiUrl}/api/books/{bookId}/editions");
            return new List<BookEditionInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting editions for book {BookId}", bookId);
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

            _logger.LogDebug("Retrieved editions for {Count} books", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Content API for batch editions. URL: {Url}, BookIds: {BookIds}",
                $"{_options.ContentApiUrl}/api/books/editions/batch", string.Join(", ", bookIds));
            return bookIds.ToDictionary(id => id, _ => new List<BookEditionInfoDto>());
        }
    }

    private record BookCoverDto
    {
        public string? CoverRef { get; init; }
    }
}
