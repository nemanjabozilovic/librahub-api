using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Infrastructure.Options;
using LibraHub.BuildingBlocks.Http;
using LibraHub.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LibraHub.Orders.Infrastructure.Clients;

public class CatalogPricingClient : ICatalogPricingClient
{
    private readonly HttpClient _httpClient;
    private readonly OrdersOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<CatalogPricingClient> _logger;

    public CatalogPricingClient(HttpClient httpClient, IOptions<OrdersOptions> options, ILogger<CatalogPricingClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Result<PricingQuote>> GetPricingQuoteAsync(
        List<Guid> bookIds,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
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

        var url = $"{_options.CatalogApiUrl}/books/pricing/quote";
        return await _httpClient.PostJsonResultAsync<PricingQuote>(
            url,
            content,
            _logger,
            notFoundResourceName: "Pricing quote",
            cancellationToken);
    }
}
