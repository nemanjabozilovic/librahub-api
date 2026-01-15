namespace LibraHub.Orders.Domain.Payments;

public enum PaymentProvider
{
    Mock = 0,
    Stripe = 1,
    PayPal = 2,
    Visa = 3,
    Mastercard = 4
}
