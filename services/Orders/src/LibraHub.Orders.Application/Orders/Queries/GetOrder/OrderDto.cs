namespace LibraHub.Orders.Application.Orders.Queries.GetOrder;

public class OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? UserDisplayName { get; init; }
    public string? UserEmail { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal VatTotal { get; init; }
    public decimal Total { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public PaymentDto? Payment { get; init; }
}

public class OrderItemDto
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal VatRate { get; init; }
    public decimal VatAmount { get; init; }
    public Guid? PromotionId { get; init; }
    public string? PromotionName { get; init; }
    public decimal? DiscountAmount { get; init; }
}

public class PaymentDto
{
    public Guid Id { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? ProviderReference { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}
