namespace LibraHub.Notifications.Application.Constants;

public static class NotificationMessages
{
    public static class BookPublished
    {
        public const string Title = "New book published";

        public static string GetMessage(string bookTitle, string authors)
        {
            return $"A new book '{bookTitle}' by {authors} has been published and is now available.";
        }
    }

    public static class AnnouncementPublished
    {
        public const string Title = "New announcement published";

        public static string GetMessage(string announcementTitle)
        {
            return $"A new announcement '{announcementTitle}' has been published for a book you might be interested in.";
        }
    }

    public static class EntitlementGranted
    {
        public const string Title = "Book added to your library";

        public static string GetMessage(Guid bookId)
        {
            return $"A new book has been added to your library. Book ID: {bookId}";
        }
    }

    public static class OrderPaid
    {
        public const string Title = "Your order has been paid";

        public static string GetMessage(Guid orderId, decimal total, string currency)
        {
            return $"Order #{orderId} has been successfully paid. Total: {total} {currency}";
        }
    }

    public static class OrderRefunded
    {
        public const string Title = "Your order has been refunded";

        public static string GetMessage(Guid orderId, string reason)
        {
            return $"Order #{orderId} has been refunded. Reason: {reason}";
        }
    }
}
