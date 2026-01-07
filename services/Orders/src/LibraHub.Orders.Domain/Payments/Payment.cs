using LibraHub.Orders.Domain.Orders;

namespace LibraHub.Orders.Domain.Payments;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }

    private Payment()
    { } // For EF Core

    public Payment(
        Guid id,
        Guid orderId,
        PaymentProvider provider,
        Money amount)
    {
        Id = id;
        OrderId = orderId;
        Provider = provider;
        Status = PaymentStatus.Pending;
        Amount = amount;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted(string providerReference)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark payment as completed when status is {Status}");
        }

        Status = PaymentStatus.Completed;
        ProviderReference = providerReference;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark payment as failed when status is {Status}");
        }

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        FailedAt = DateTime.UtcNow;
    }

    public void MarkAsCancelled()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot cancel payment when status is {Status}");
        }

        Status = PaymentStatus.Cancelled;
        FailedAt = DateTime.UtcNow;
    }
}
