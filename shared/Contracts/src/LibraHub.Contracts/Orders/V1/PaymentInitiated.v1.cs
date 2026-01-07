namespace LibraHub.Contracts.Orders.V1;

public class PaymentInitiatedV1
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset InitiatedAt { get; set; }
}
