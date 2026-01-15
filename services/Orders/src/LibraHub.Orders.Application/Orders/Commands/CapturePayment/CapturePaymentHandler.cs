using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Errors;
using LibraHub.Orders.Domain.Orders;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Orders.Application.Orders.Commands.CapturePayment;

public class CapturePaymentHandler(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    IOutboxWriter outboxWriter,
    ICurrentUser currentUser,
    IClock clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CapturePaymentCommand, Result>
{
    public async Task<Result> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
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

        if (order.Status != OrderStatus.PaymentPending)
        {
            return Result.Failure(Error.Validation(OrdersErrors.Order.InvalidStatus));
        }

        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment == null)
        {
            return Result.Failure(Error.NotFound(OrdersErrors.Payment.NotFound));
        }

        if (payment.OrderId != order.Id)
        {
            return Result.Failure(Error.Validation("Payment does not belong to this order"));
        }

        if (payment.ProviderReference != request.ProviderReference)
        {
            return Result.Failure(Error.Validation("Provider reference mismatch"));
        }

        var paymentResult = await paymentGateway.CapturePaymentAsync(
            request.ProviderReference,
            cancellationToken);

        if (!paymentResult.Success)
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                payment.MarkAsFailed(paymentResult.FailureReason ?? "Payment capture failed");
                await paymentRepository.UpdateAsync(payment, ct);

                order.Cancel("Payment capture failed");
                await orderRepository.UpdateAsync(order, ct);
            }, cancellationToken);

            return Result.Failure(new Error("INTERNAL_ERROR", $"Payment capture failed: {paymentResult.FailureReason}"));
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            payment.MarkAsCompleted(paymentResult.ProviderReference!);
            await paymentRepository.UpdateAsync(payment, ct);

            order.MarkAsPaid();
            await orderRepository.UpdateAsync(order, ct);

            await outboxWriter.WriteAsync(
                new Contracts.Orders.V1.OrderPaidV1
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    PaymentId = payment.Id,
                    Items = order.Items.Select(i => new Contracts.Orders.V1.OrderItemDto
                    {
                        BookId = i.BookId,
                        BookTitle = i.BookTitle,
                        BasePrice = i.BasePrice.Amount,
                        FinalPrice = i.FinalPrice.Amount,
                        VatRate = i.VatRate,
                        VatAmount = i.VatAmount.Amount,
                        PromotionId = i.PromotionId,
                        PromotionName = i.PromotionName,
                        DiscountAmount = i.DiscountAmount
                    }).ToList(),
                    Subtotal = order.Subtotal.Amount,
                    VatTotal = order.VatTotal.Amount,
                    Total = order.Total.Amount,
                    Currency = order.Currency,
                    PaidAt = clock.UtcNowOffset
                },
                Contracts.Common.EventTypes.OrderPaid,
                ct);
        }, cancellationToken);

        return Result.Success();
    }
}
