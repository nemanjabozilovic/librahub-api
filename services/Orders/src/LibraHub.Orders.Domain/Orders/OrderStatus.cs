namespace LibraHub.Orders.Domain.Orders;

public enum OrderStatus
{
    Created = 0,
    PaymentPending = 1,
    Paid = 2,
    Cancelled = 3,
    Refunded = 4
}
