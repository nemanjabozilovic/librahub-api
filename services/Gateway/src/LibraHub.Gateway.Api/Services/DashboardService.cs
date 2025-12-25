using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Http;
using LibraHub.Gateway.Api.Dtos.Dashboard;
using LibraHub.Gateway.Api.Options;
using Microsoft.Extensions.Options;

namespace LibraHub.Gateway.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly ServiceClientHelper _serviceClient;
    private readonly ServicesOptions _servicesOptions;
    private readonly StatisticsCacheHelper _cache;

    public DashboardService(
        ServiceClientHelper serviceClient,
        IOptions<ServicesOptions> servicesOptions,
        StatisticsCacheHelper cache)
    {
        _serviceClient = serviceClient;
        _servicesOptions = servicesOptions.Value;
        _cache = cache;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(string authorizationToken, CancellationToken cancellationToken = default)
    {
        var cacheKey = StatisticsCacheHelper.GetDashboardSummaryKey();
        var cachedSummary = await _cache.GetAsync<DashboardSummaryDto>(cacheKey, cancellationToken);

        if (cachedSummary != null)
        {
            return cachedSummary;
        }

        var (usersTask, booksTask, ordersTask, entitlementsTask) = FetchAllStatisticsAsync(authorizationToken, cancellationToken);
        await Task.WhenAll(usersTask, booksTask, ordersTask, entitlementsTask);

        var summary = await BuildDashboardSummaryAsync(usersTask, booksTask, ordersTask, entitlementsTask);

        await _cache.SetAsync(cacheKey, summary, cancellationToken: cancellationToken);

        return summary;
    }

    private (Task<UserStatisticsDto?> Users, Task<BookStatisticsDto?> Books, Task<OrderStatisticsDto?> Orders, Task<EntitlementStatisticsDto?> Entitlements)
        FetchAllStatisticsAsync(string token, CancellationToken cancellationToken)
    {
        return (
            _serviceClient.GetAsync<UserStatisticsDto>(_servicesOptions.Identity, "admin/statistics/users", token, cancellationToken),
            _serviceClient.GetAsync<BookStatisticsDto>(_servicesOptions.Catalog, "admin/statistics/books", token, cancellationToken),
            _serviceClient.GetAsync<OrderStatisticsDto>(_servicesOptions.Orders, "admin/statistics/orders", token, cancellationToken),
            _serviceClient.GetAsync<EntitlementStatisticsDto>(_servicesOptions.Library, "api/admin/statistics/entitlements", token, cancellationToken)
        );
    }

    private async Task<DashboardSummaryDto> BuildDashboardSummaryAsync(
        Task<UserStatisticsDto?> usersTask,
        Task<BookStatisticsDto?> booksTask,
        Task<OrderStatisticsDto?> ordersTask,
        Task<EntitlementStatisticsDto?> entitlementsTask)
    {
        var users = await usersTask;
        var books = await booksTask;
        var orders = await ordersTask;
        var entitlements = await entitlementsTask;

        return new DashboardSummaryDto
        {
            Users = users,
            Books = books,
            Orders = orders,
            Entitlements = entitlements,
            Revenue = BuildRevenueDto(orders)
        };
    }

    private RevenueDto BuildRevenueDto(OrderStatisticsDto? orderStats)
    {
        return new RevenueDto
        {
            Total = orderStats?.TotalRevenue ?? 0,
            Last30Days = orderStats?.Last30Days?.Revenue ?? 0,
            Last7Days = orderStats?.Last7Days?.Revenue ?? 0,
            Currency = orderStats?.Currency ?? "USD"
        };
    }
}

