using Microsoft.Extensions.Primitives;

namespace LibraHub.BuildingBlocks.Correlation;

public sealed class CorrelationIdHeaderHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationContext.Current;
        if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(CorrelationIdHeader))
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdHeader, new StringValues(correlationId).ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}

