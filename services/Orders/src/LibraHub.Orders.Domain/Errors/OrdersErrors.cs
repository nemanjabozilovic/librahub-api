namespace LibraHub.Orders.Domain.Errors;

public static class OrdersErrors
{
    public static class Order
    {
        public const string NotFound = "Order not found";
        public const string AlreadyPaid = "Order is already paid";
        public const string AlreadyCancelled = "Order is already cancelled";
        public const string AlreadyRefunded = "Order is already refunded";
        public const string CannotCancel = "Order cannot be cancelled in current status";
        public const string CannotRefund = "Order cannot be refunded in current status";
        public const string EmptyItems = "Order must contain at least one item";
        public const string InvalidStatus = "Invalid order status for this operation";
    }

    public static class Payment
    {
        public const string NotFound = "Payment not found";
        public const string AlreadyCompleted = "Payment is already completed";
        public const string AlreadyFailed = "Payment is already failed";
        public const string InvalidStatus = "Invalid payment status for this operation";
    }

    public static class Book
    {
        public const string NotFound = "Book not found";
        public const string NotPublished = "Book is not published";
        public const string IsFree = "Book is free and cannot be purchased";
        public const string AlreadyOwned = "User already owns this book";
        public const string Removed = "Book has been removed";
    }

    public static class User
    {
        public const string NotAuthenticated = "User is not authenticated";
        public const string NotActive = "User is not active";
        public const string EmailNotVerified = "User email is not verified";
    }
}
