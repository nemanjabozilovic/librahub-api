using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Options;
using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Results;
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

    public async Task<Result<string?>> GetBookCoverRefAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.ContentApiUrl}/api/books/{bookId}/cover";
            _logger.LogDebug("Calling Content API for cover ref: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("No cover found for book {BookId}", bookId);
                return Result.Success<string?>(null);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Content API call failed. Method=GET Url={Url} StatusCode={StatusCode} CorrelationId={CorrelationId} Body={Body}",
                    url,
                    (int)response.StatusCode,
                    CorrelationContext.Current,
                    body);
                return Result.Failure<string?>(Error.Unexpected($"Downstream call failed with status {(int)response.StatusCode}"));
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var coverDto = JsonSerializer.Deserialize<BookCoverDto>(content, _jsonOptions);

            _logger.LogDebug("Retrieved cover ref for book {BookId}: {CoverRef}", bookId, coverDto?.CoverRef);
            return Result.Success<string?>(coverDto?.CoverRef);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Content API for book {BookId} cover. URL: {Url}", bookId, $"{_options.ContentApiUrl}/api/books/{bookId}/cover");
            return Result.Failure<string?>(Error.Unexpected("HTTP request failed"));
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Content API for book {BookId} cover. URL: {Url}. CorrelationId={CorrelationId}",
                bookId, $"{_options.ContentApiUrl}/api/books/{bookId}/cover", CorrelationContext.Current);
            return Result.Failure<string?>(Error.Unexpected("HTTP request timed out"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting cover for book {BookId}", bookId);
            return Result.Failure<string?>(Error.Unexpected("Unexpected error"));
        }
    }

    public async Task<Result<List<BookEditionInfoDto>>> GetBookEditionsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.ContentApiUrl}/api/books/{bookId}/editions";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("No editions found for book {BookId}", bookId);
                return Result.Success(new List<BookEditionInfoDto>());
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Content API call failed. Method=GET Url={Url} StatusCode={StatusCode} CorrelationId={CorrelationId} Body={Body}",
                    url,
                    (int)response.StatusCode,
                    CorrelationContext.Current,
                    body);
                return Result.Failure<List<BookEditionInfoDto>>(Error.Unexpected($"Downstream call failed with status {(int)response.StatusCode}"));
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var editions = JsonSerializer.Deserialize<List<BookEditionInfoDto>>(content, _jsonOptions)
                ?? new List<BookEditionInfoDto>();

            _logger.LogDebug("Retrieved {Count} editions for book {BookId}", editions.Count, bookId);
            return Result.Success(editions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Content API for book {BookId} editions. URL: {Url}", bookId, $"{_options.ContentApiUrl}/api/books/{bookId}/editions");
            return Result.Failure<List<BookEditionInfoDto>>(Error.Unexpected("HTTP request failed"));
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Content API for book {BookId} editions. URL: {Url}. CorrelationId={CorrelationId}",
                bookId, $"{_options.ContentApiUrl}/api/books/{bookId}/editions", CorrelationContext.Current);
            return Result.Failure<List<BookEditionInfoDto>>(Error.Unexpected("HTTP request timed out"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting editions for book {BookId}", bookId);
            return Result.Failure<List<BookEditionInfoDto>>(Error.Unexpected("Unexpected error"));
        }
    }

    public async Task<Result<Dictionary<Guid, List<BookEditionInfoDto>>>> GetBookEditionsBatchAsync(List<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
        {
            return Result.Success(new Dictionary<Guid, List<BookEditionInfoDto>>());
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
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Content API call failed. Method=POST Url={Url} StatusCode={StatusCode} CorrelationId={CorrelationId} Body={Body}",
                    url,
                    (int)response.StatusCode,
                    CorrelationContext.Current,
                    body);
                return Result.Failure<Dictionary<Guid, List<BookEditionInfoDto>>>(
                    Error.Unexpected($"Downstream call failed with status {(int)response.StatusCode}"));
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<Dictionary<Guid, List<BookEditionInfoDto>>>(responseContent, _jsonOptions)
                ?? new Dictionary<Guid, List<BookEditionInfoDto>>();

            _logger.LogDebug("Retrieved editions for {Count} books", result.Count);
            return Result.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Content API for batch editions. URL: {Url}, BookIds: {BookIds}",
                $"{_options.ContentApiUrl}/api/books/editions/batch", string.Join(", ", bookIds));
            return Result.Failure<Dictionary<Guid, List<BookEditionInfoDto>>>(Error.Unexpected("HTTP request failed"));
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Content API for batch editions. URL: {Url}. CorrelationId={CorrelationId}",
                $"{_options.ContentApiUrl}/api/books/editions/batch", CorrelationContext.Current);
            return Result.Failure<Dictionary<Guid, List<BookEditionInfoDto>>>(Error.Unexpected("HTTP request timed out"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Content API for batch editions. URL: {Url}, BookIds: {BookIds}",
                $"{_options.ContentApiUrl}/api/books/editions/batch", string.Join(", ", bookIds));
            return Result.Failure<Dictionary<Guid, List<BookEditionInfoDto>>>(Error.Unexpected("Unexpected error"));
        }
    }

    private record BookCoverDto
    {
        public string? CoverRef { get; init; }
    }
}
