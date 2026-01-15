using Microsoft.Extensions.Options;

namespace LibraHub.BuildingBlocks.InternalAccess;

public sealed class InternalAccessHeaderHandler(IOptions<InternalAccessOptions> options) : DelegatingHandler
{
    private readonly InternalAccessOptions _options = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(InternalAccessConstants.HeaderName))
        {
            request.Headers.Add(InternalAccessConstants.HeaderName, _options.ApiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

