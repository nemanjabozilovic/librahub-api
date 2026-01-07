namespace LibraHub.Contracts.Orders.V1;

public class OrderCancelledV1
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CancelledAt { get; set; }
}
