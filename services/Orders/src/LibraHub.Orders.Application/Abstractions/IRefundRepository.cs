using LibraHub.Orders.Domain.Refunds;

namespace LibraHub.Orders.Application.Abstractions;

public interface IRefundRepository
{
    Task AddAsync(Refund refund, CancellationToken cancellationToken = default);
    Task<Refund?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default);
}

