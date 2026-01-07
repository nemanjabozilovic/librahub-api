using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Orders.Application.Orders.Commands.CancelOrder;

public class CancelOrderHandler(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IOutboxWriter outboxWriter,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(OrdersErrors.User.NotAuthenticated));
        }

        var userId = currentUser.UserId.Value;

        var order = await orderRepository.GetByIdAndUserIdAsync(request.OrderId, userId, cancellationToken);
        if (order == null)
        {
            return Result.Failure(Error.NotFound(OrdersErrors.Order.NotFound));
        }

        if (!order.CanBeCancelled)
        {
            return Result.Failure(Error.Validation(OrdersErrors.Order.CannotCancel));
        }

        order.Cancel(request.Reason);
        await orderRepository.UpdateAsync(order, cancellationToken);

        // Cancel payment if exists
        var payment = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (payment != null && payment.Status == LibraHub.Orders.Domain.Payments.PaymentStatus.Pending)
        {
            payment.MarkAsCancelled();
            await paymentRepository.UpdateAsync(payment, cancellationToken);
        }

        // Publish event
        await outboxWriter.WriteAsync(
            new Contracts.Orders.V1.OrderCancelledV1
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Reason = request.Reason,
                CancelledAt = clock.UtcNowOffset
            },
            Contracts.Common.EventTypes.OrderCancelled,
            cancellationToken);

        return Result.Success();
    }
}
