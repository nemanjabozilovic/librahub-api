using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Payments;
using LibraHub.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Orders.Infrastructure.Repositories;

public class PaymentRepository(OrdersDbContext context) : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await context.Payments.AddAsync(payment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        context.Payments.Update(payment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
