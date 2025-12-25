using LibraHub.Gateway.Api.Dtos.Dashboard;

namespace LibraHub.Gateway.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(string authorizationToken, CancellationToken cancellationToken = default);
}

