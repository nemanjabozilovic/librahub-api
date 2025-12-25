using LibraHub.Orders.Domain.Orders;

namespace LibraHub.Orders.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAndUserIdAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default);

    Task<List<Order>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<OrderStatisticsResult> GetStatisticsAsync(DateTime last30Days, DateTime last7Days, DateTime now, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

