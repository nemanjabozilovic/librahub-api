using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Orders.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;

    public OrderRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetByIdAndUserIdAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);
    }

    public async Task<List<Order>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .CountAsync(o => o.UserId == userId, cancellationToken);
    }

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders.CountAsync(cancellationToken);
    }

    public async Task<List<Order>> GetAllAsync(int skip, int take, DateTime? fromDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(DateTime? fromDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<OrderStatisticsResult> GetStatisticsAsync(DateTime last30Days, DateTime last7Days, DateTime now, CancellationToken cancellationToken = default)
    {
        var total = await _context.Orders.CountAsync(cancellationToken);
        var paid = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Paid, cancellationToken);
        var pending = await _context.Orders.CountAsync(o => o.Status == OrderStatus.PaymentPending, cancellationToken);
        var cancelled = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled, cancellationToken);
        var refunded = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Refunded, cancellationToken);

        var totalRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Paid)
            .SumAsync(o => o.Total.Amount, cancellationToken);

        var last30DaysRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Paid && o.CreatedAt >= last30Days && o.CreatedAt <= now)
            .SumAsync(o => o.Total.Amount, cancellationToken);

        var last7DaysRevenue = await _context.Orders
            .Where(o => o.Status == OrderStatus.Paid && o.CreatedAt >= last7Days && o.CreatedAt <= now)
            .SumAsync(o => o.Total.Amount, cancellationToken);

        var last30DaysCount = await _context.Orders
            .CountAsync(o => o.Status == OrderStatus.Paid && o.CreatedAt >= last30Days && o.CreatedAt <= now, cancellationToken);

        var last7DaysCount = await _context.Orders
            .CountAsync(o => o.Status == OrderStatus.Paid && o.CreatedAt >= last7Days && o.CreatedAt <= now, cancellationToken);

        return new OrderStatisticsResult
        {
            Total = total,
            Paid = paid,
            Pending = pending,
            Cancelled = cancelled,
            Refunded = refunded,
            Last30Days = new PeriodStatisticsData
            {
                Count = last30DaysCount,
                Revenue = last30DaysRevenue
            },
            Last7Days = new PeriodStatisticsData
            {
                Count = last7DaysCount,
                Revenue = last7DaysRevenue
            },
            TotalRevenue = totalRevenue,
            Currency = LibraHub.BuildingBlocks.Constants.Currency.USD
        };
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

