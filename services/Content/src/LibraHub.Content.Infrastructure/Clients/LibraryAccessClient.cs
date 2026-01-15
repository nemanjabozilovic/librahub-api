using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraHub.Content.Infrastructure.Clients;

public class LibraryAccessClient : ILibraryAccessClient
{
    private readonly HttpClient _httpClient;
    private readonly ReadAccessOptions _options;
    private readonly ILogger<LibraryAccessClient> _logger;

    public LibraryAccessClient(HttpClient httpClient, IOptions<ReadAccessOptions> options, ILogger<LibraryAccessClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<bool>> UserOwnsBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.LibraryApiUrl}/api/access/check?userId={userId}&bookId={bookId}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result.Success(false);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Library access check failed with {StatusCode}. CorrelationId={CorrelationId}. Body={Body}",
                    (int)response.StatusCode,
                    CorrelationContext.Current,
                    body);
                return Result.Failure<bool>(Error.Unexpected($"Downstream call failed with status {(int)response.StatusCode}"));
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = System.Text.Json.JsonSerializer.Deserialize<AccessCheckResult>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return Result.Success(result?.HasAccess ?? false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Library access check request failed. CorrelationId={CorrelationId}", CorrelationContext.Current);
            return Result.Failure<bool>(Error.Unexpected("HTTP request failed"));
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Library access check request timed out. CorrelationId={CorrelationId}", CorrelationContext.Current);
            return Result.Failure<bool>(Error.Unexpected("HTTP request timed out"));
        }
    }

    private record AccessCheckResult
    {
        public bool HasAccess { get; init; }
    }
}
