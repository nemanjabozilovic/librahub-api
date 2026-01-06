using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Orders.Application.Abstractions;
using LibraHub.Orders.Domain.Errors;
using LibraHub.Orders.Domain.Orders;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Orders.Application.Orders.Queries.GetOrder;

public class GetOrderHandler(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IIdentityClient identityClient,
    ICurrentUser currentUser) : IRequestHandler<GetOrderQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure<OrderDto>(Error.Unauthorized(OrdersErrors.User.NotAuthenticated));
        }

        Order? order;

        if (currentUser.IsInRole("Admin"))
        {
            order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        }
        else
        {
            var userIdResult = currentUser.RequireUserId(OrdersErrors.User.NotAuthenticated);
            if (userIdResult.IsFailure)
            {
                return Result.Failure<OrderDto>(userIdResult.Error!);
            }

            var userId = userIdResult.Value;
            order = await orderRepository.GetByIdAndUserIdAsync(request.OrderId, userId, cancellationToken);
        }

        if (order == null)
        {
            return Result.Failure<OrderDto>(Error.NotFound(OrdersErrors.Order.NotFound));
        }

        var payment = await paymentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var userInfo = await identityClient.GetUserInfoAsync(order.UserId, cancellationToken);

        var dto = new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            UserDisplayName = userInfo?.DisplayName,
            UserEmail = userInfo?.Email,
            Status = order.Status.ToString(),
            Subtotal = order.Subtotal.Amount,
            VatTotal = order.VatTotal.Amount,
            Total = order.Total.Amount,
            Currency = order.Currency,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CancelledAt = order.CancelledAt,
            CancellationReason = order.CancellationReason,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
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
            Payment = payment != null ? new PaymentDto
            {
                Id = payment.Id,
                Provider = payment.Provider.ToString(),
                Status = payment.Status.ToString(),
                Amount = payment.Amount.Amount,
                ProviderReference = payment.ProviderReference,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt
            } : null
        };

        return Result.Success(dto);
    }
}

