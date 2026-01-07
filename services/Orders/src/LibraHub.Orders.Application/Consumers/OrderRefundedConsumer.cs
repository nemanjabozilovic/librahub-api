using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LibraHub.Orders.Application.Consumers;

public class OrderRefundedConsumer(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IRefundRepository refundRepository,
    IUnitOfWork unitOfWork,
    ILogger<OrderRefundedConsumer> logger)
{
    public async Task HandleAsync(OrderRefundedV1 @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing OrderRefunded event for OrderId: {OrderId}, RefundId: {RefundId}",
            @event.OrderId, @event.RefundId);

        var order = await orderRepository.GetByIdAsync(@event.OrderId, cancellationToken);
        if (order == null)
        {
            logger.LogWarning("Order not found for OrderId: {OrderId} in OrderRefunded event", @event.OrderId);
            return;
        }

        if (order.Status != Domain.Orders.OrderStatus.Paid)
        {
            logger.LogWarning("Order {OrderId} is not in Paid status, current status: {Status}. Skipping refund processing.",
                @event.OrderId, order.Status);
            return;
        }

        var payment = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (payment == null)
        {
            logger.LogWarning("Payment not found for OrderId: {OrderId} in OrderRefunded event", @event.OrderId);
            return;
        }

        var existingRefund = await refundRepository.GetByIdAsync(@event.RefundId, cancellationToken);
        if (existingRefund != null)
        {
            logger.LogInformation("Refund {RefundId} already exists for OrderId: {OrderId}, skipping processing",
                @event.RefundId, @event.OrderId);
            return;
        }

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var refund = new Domain.Refunds.Refund(
                    @event.RefundId,
                    order.Id,
                    payment.Id,
                    @event.Reason,
                    @event.RefundedBy);

                await refundRepository.AddAsync(refund, ct);

                order.MarkAsRefunded();
                await orderRepository.UpdateAsync(order, ct);
            }, cancellationToken);

            logger.LogInformation("Completed processing OrderRefunded event for OrderId: {OrderId}, RefundId: {RefundId}",
                @event.OrderId, @event.RefundId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process OrderRefunded event for OrderId: {OrderId}, RefundId: {RefundId}",
                @event.OrderId, @event.RefundId);
            throw;
        }
    }
}
