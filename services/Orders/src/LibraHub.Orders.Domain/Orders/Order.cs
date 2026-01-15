namespace LibraHub.Orders.Domain.Orders;

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Subtotal { get; private set; } = null!;
    public Money VatTotal { get; private set; } = null!;
    public Money Total { get; private set; } = null!;
    public string Currency { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    { }

    public Order(
        Guid id,
        Guid userId,
        List<OrderItem> items,
        Money subtotal,
        Money vatTotal,
        Money total)
    {
        if (items == null || items.Count == 0)
        {
            throw new ArgumentException("Order must have at least one item", nameof(items));
        }

        if (subtotal.Currency != vatTotal.Currency || subtotal.Currency != total.Currency)
        {
            throw new ArgumentException("All money amounts must have the same currency");
        }

        Id = id;
        UserId = userId;
        Status = OrderStatus.Created;
        _items = items;
        Subtotal = subtotal;
        VatTotal = vatTotal;
        Total = total;
        Currency = total.Currency;
        CreatedAt = DateTime.UtcNow;

        foreach (var item in _items)
        {
            item.SetOrderId(id);
        }
    }

    public void StartPayment()
    {
        if (Status != OrderStatus.Created)
        {
            throw new InvalidOperationException($"Cannot start payment for order in {Status} status");
        }

        Status = OrderStatus.PaymentPending;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid()
    {
        if (Status != OrderStatus.PaymentPending)
        {
            throw new InvalidOperationException($"Cannot mark order as paid when status is {Status}");
        }

        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status != OrderStatus.Created && Status != OrderStatus.PaymentPending)
        {
            throw new InvalidOperationException($"Cannot cancel order in {Status} status");
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        if (Status != OrderStatus.Paid)
        {
            throw new InvalidOperationException($"Cannot refund order in {Status} status");
        }

        Status = OrderStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeCancelled => Status == OrderStatus.Created || Status == OrderStatus.PaymentPending;
    public bool CanBeRefunded => Status == OrderStatus.Paid;
}
