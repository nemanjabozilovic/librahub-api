using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Orders.Infrastructure.Clients;

public class LibraryOwnershipClient : ILibraryOwnershipClient
{
    private readonly HttpClient _httpClient;
    private readonly OrdersOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<LibraryOwnershipClient> _logger;

    public LibraryOwnershipClient(HttpClient httpClient, IOptions<OrdersOptions> options, ILogger<LibraryOwnershipClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Result<bool>> UserOwnsBookAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken = default)
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
            var result = JsonSerializer.Deserialize<AccessCheckResult>(content, _jsonOptions);

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

    public async Task<Result<List<Guid>>> GetOwnedBookIdsAsync(
        Guid userId,
        List<Guid> bookIds,
        CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
        {
            return Result.Success(new List<Guid>());
        }

        var distinct = bookIds.Distinct().ToList();
        var checks = distinct.Select(async bookId =>
        {
            var ownsResult = await UserOwnsBookAsync(userId, bookId, cancellationToken);
            return (bookId, ownsResult);
        });

        var results = await Task.WhenAll(checks);
        var firstFailure = results.FirstOrDefault(x => x.ownsResult.IsFailure).ownsResult;
        if (firstFailure != null && firstFailure.IsFailure)
        {
            return Result.Failure<List<Guid>>(firstFailure.Error ?? Error.Unexpected("Downstream call failed"));
        }

        var owned = results
            .Where(x => x.ownsResult.IsSuccess && x.ownsResult.Value)
            .Select(x => x.bookId)
            .ToList();

        return Result.Success(owned);
    }

    private record AccessCheckResult
    {
        public bool HasAccess { get; init; }
    }
}
