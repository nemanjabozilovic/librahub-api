namespace LibraHub.Orders.Domain.Orders;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid BookId { get; private set; }
    public string BookTitle { get; private set; } = string.Empty;
    public Money BasePrice { get; private set; } = null!;
    public Money FinalPrice { get; private set; } = null!;
    public decimal VatRate { get; private set; }
    public Money VatAmount { get; private set; } = null!;
    public Guid? PromotionId { get; private set; }
    public string? PromotionName { get; private set; }
    public decimal? DiscountAmount { get; private set; }

    private OrderItem()
    { }

    public OrderItem(
        Guid id,
        Guid orderId,
        Guid bookId,
        string bookTitle,
        Money basePrice,
        Money finalPrice,
        decimal vatRate,
        Money vatAmount,
        Guid? promotionId = null,
        string? promotionName = null,
        decimal? discountAmount = null)
    {
        Id = id;
        OrderId = orderId;
        BookId = bookId;
        BookTitle = bookTitle;
        BasePrice = basePrice;
        FinalPrice = finalPrice;
        VatRate = vatRate;
        VatAmount = vatAmount;
        PromotionId = promotionId;
        PromotionName = promotionName;
        DiscountAmount = discountAmount;
    }

    public void SetOrderId(Guid orderId)
    {
        OrderId = orderId;
    }
}
