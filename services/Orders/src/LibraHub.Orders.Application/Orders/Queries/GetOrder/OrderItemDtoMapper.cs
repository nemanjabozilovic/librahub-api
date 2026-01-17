using LibraHub.Orders.Domain.Orders;

namespace LibraHub.Orders.Application.Orders.Queries.GetOrder;

public static class OrderItemDtoMapper
{
    public static OrderItemDto MapFromOrderItem(OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id,
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
