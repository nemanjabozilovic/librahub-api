using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace LibraHub.BuildingBlocks.Http;

public static class HttpClientResultExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<Result<T>> GetJsonResultAsync<T>(
        this HttpClient httpClient,
        string url,
        ILogger logger,
        string notFoundResourceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            return await MapJsonResponseAsync<T>(response, logger, url, notFoundResourceName, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogHttpFailure(logger, ex, HttpMethod.Get, url);
            return Result.Failure<T>(Error.Unexpected("HTTP request failed"));
        }
        catch (TaskCanceledException ex)
        {
            LogHttpFailure(logger, ex, HttpMethod.Get, url);
            return Result.Failure<T>(Error.Unexpected("HTTP request timed out"));
        }
    }

    public static async Task<Result<T>> PostJsonResultAsync<T>(
        this HttpClient httpClient,
        string url,
        HttpContent content,
        ILogger logger,
        string notFoundResourceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PostAsync(url, content, cancellationToken);
            return await MapJsonResponseAsync<T>(response, logger, url, notFoundResourceName, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogHttpFailure(logger, ex, HttpMethod.Post, url);
            return Result.Failure<T>(Error.Unexpected("HTTP request failed"));
        }
        catch (TaskCanceledException ex)
        {
            LogHttpFailure(logger, ex, HttpMethod.Post, url);
            return Result.Failure<T>(Error.Unexpected("HTTP request timed out"));
        }
    }

    private static async Task<Result<T>> MapJsonResponseAsync<T>(
        HttpResponseMessage response,
        ILogger logger,
        string url,
        string notFoundResourceName,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Result.Failure<T>(Error.NotFound(notFoundResourceName));
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Result.Failure<T>(Error.Unauthorized("Unauthorized"));
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return Result.Failure<T>(Error.Forbidden("Forbidden"));
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response, cancellationToken);
            logger.LogWarning(
                "HTTP {Method} {Url} failed with {StatusCode}. CorrelationId={CorrelationId}. Body={Body}",
                response.RequestMessage?.Method.Method ?? "UNKNOWN",
                url,
                (int)response.StatusCode,
                CorrelationContext.Current,
                body);

            return Result.Failure<T>(Error.Unexpected($"Downstream call failed with status {(int)response.StatusCode}"));
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure<T>(Error.Unexpected("Downstream returned empty response"));
        }

        var model = JsonSerializer.Deserialize<T>(content, DefaultJsonOptions);
        if (model == null)
        {
            logger.LogWarning(
                "HTTP {Method} {Url} returned invalid JSON. CorrelationId={CorrelationId}. Body={Body}",
                response.RequestMessage?.Method.Method ?? "UNKNOWN",
                url,
                CorrelationContext.Current,
                content);
            return Result.Failure<T>(Error.Unexpected("Downstream returned invalid response"));
        }

        return Result.Success(model);
    }

    private static async Task<string?> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static void LogHttpFailure(ILogger logger, Exception ex, HttpMethod method, string url)
    {
        logger.LogError(
            ex,
            "HTTP {Method} {Url} request failed. CorrelationId={CorrelationId}",
            method.Method,
            url,
            CorrelationContext.Current);
    }
}

