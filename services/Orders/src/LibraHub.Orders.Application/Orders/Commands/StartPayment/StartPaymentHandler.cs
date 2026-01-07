using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Errors;
using LibraHub.Orders.Domain.Orders;
using LibraHub.Orders.Domain.Payments;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Orders.Application.Orders.Commands.StartPayment;

public class StartPaymentHandler(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    IOutboxWriter outboxWriter,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<StartPaymentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(StartPaymentCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(OrdersErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return userIdResult;
        }

        var userId = userIdResult.Value;

        var order = await orderRepository.GetByIdAndUserIdAsync(request.OrderId, userId, cancellationToken);
        if (order == null)
        {
            return Result.Failure<Guid>(Error.NotFound(OrdersErrors.Order.NotFound));
        }

        if (order.Status != OrderStatus.Created)
        {
            return Result.Failure<Guid>(Error.Validation(OrdersErrors.Order.InvalidStatus));
        }

        if (!Enum.TryParse<PaymentProvider>(request.Provider, ignoreCase: true, out var provider))
        {
            return Result.Failure<Guid>(Error.Validation("Invalid payment provider"));
        }

        var paymentResult = await paymentGateway.InitiatePaymentAsync(
            order.Id,
            order.Total,
            provider,
            cancellationToken);

        if (!paymentResult.Success)
        {
            return Result.Failure<Guid>(new Error("INTERNAL_ERROR", $"Payment initiation failed: {paymentResult.FailureReason}"));
        }

        var payment = new Payment(
            Guid.NewGuid(),
            order.Id,
            provider,
            order.Total);

        payment.MarkAsCompleted(paymentResult.ProviderReference!);

        await paymentRepository.AddAsync(payment, cancellationToken);

        order.StartPayment();
        await orderRepository.UpdateAsync(order, cancellationToken);
        await outboxWriter.WriteAsync(
            new Contracts.Orders.V1.PaymentInitiatedV1
            {
                OrderId = order.Id,
                PaymentId = payment.Id,
                Provider = provider.ToString(),
                Amount = order.Total.Amount,
                Currency = order.Currency,
                InitiatedAt = clock.UtcNowOffset
            },
            Contracts.Common.EventTypes.PaymentInitiated,
            cancellationToken);

        return Result.Success(payment.Id);
    }
}
