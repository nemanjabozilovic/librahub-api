using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LibraHub.Identity.Infrastructure.Security;

public class RecaptchaService(
    IHttpClientFactory httpClientFactory,
    IOptions<RecaptchaOptions> options,
    ILogger<RecaptchaService> logger) : IRecaptchaService
{
    private readonly RecaptchaOptions _options = options.Value;

    public async Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("reCAPTCHA token is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            logger.LogWarning("reCAPTCHA SecretKey is not configured");
            return false;
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(
                $"{_options.VerifyUrl}?secret={_options.SecretKey}&response={token}",
                null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("reCAPTCHA verification request failed with status {StatusCode}", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                logger.LogWarning("Failed to deserialize reCAPTCHA response");
                return false;
            }

            if (!result.Success)
            {
                logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}",
                    result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "Unknown");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during reCAPTCHA verification");
            return false;
        }
    }

    private class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
