using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Refunds;
using LibraHub.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Orders.Infrastructure.Repositories;

public class RefundRepository(OrdersDbContext context) : IRefundRepository
{
    public async Task AddAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await context.Refunds.AddAsync(refund, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Refund?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
    {
        return await context.Refunds
            .FirstOrDefaultAsync(r => r.Id == refundId, cancellationToken);
    }
}
