using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Orders.Infrastructure.Clients;

public class CatalogPricingClient : ICatalogPricingClient
{
    private readonly HttpClient _httpClient;
    private readonly OrdersOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public CatalogPricingClient(HttpClient httpClient, IOptions<OrdersOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<PricingQuote?> GetPricingQuoteAsync(
        List<Guid> bookIds,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                BookIds = bookIds,
                UserId = userId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.CatalogApiUrl}/api/books/pricing/quote",
                content,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var quote = JsonSerializer.Deserialize<PricingQuote>(responseContent, _jsonOptions);

            return quote;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
