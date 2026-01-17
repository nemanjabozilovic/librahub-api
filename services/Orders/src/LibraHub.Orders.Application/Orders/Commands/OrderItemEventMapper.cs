using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Domain.Orders;

namespace LibraHub.Orders.Application.Orders.Commands;

public static class OrderItemEventMapper
{
    public static OrderItemDto MapToEventDto(OrderItem item)
    {
        return new OrderItemDto
        {
            BookId = item.BookId,
            BookTitle = item.BookTitle,
            BasePrice = item.BasePrice.Amount,
            FinalPrice = item.FinalPrice.Amount,
            VatRate = item.VatRate,
            VatAmount = item.VatAmount.Amount,
            PromotionId = item.PromotionId,
            PromotionName = item.PromotionName,
            DiscountAmount = item.DiscountAmount
        };
    }
}
