namespace LibraHub.Contracts.Orders.V1;

public class OrderRefundedV1
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public Guid RefundId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid RefundedBy { get; set; }
    public DateTimeOffset RefundedAt { get; set; }
}
