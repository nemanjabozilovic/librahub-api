namespace LibraHub.Orders.Domain.Refunds;

public class Refund
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid PaymentId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid RefundedBy { get; private set; }
    public DateTime RefundedAt { get; private set; }

    private Refund()
    { } // For EF Core

    public Refund(
        Guid id,
        Guid orderId,
        Guid paymentId,
        string reason,
        Guid refundedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Refund reason cannot be empty", nameof(reason));
        }

        Id = id;
        OrderId = orderId;
        PaymentId = paymentId;
        Reason = reason;
        RefundedBy = refundedBy;
        RefundedAt = DateTime.UtcNow;
    }
}
