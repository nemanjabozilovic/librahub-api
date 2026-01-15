namespace LibraHub.Contracts.Common;

public static class EventTypes
{
    public const string UserRegistered = "Identity.UserRegistered.v1";
    public const string EmailVerified = "Identity.EmailVerified.v1";
    public const string UserRemoved = "Identity.UserRemoved.v1";
    public const string RoleAssigned = "Identity.RoleAssigned.v1";
    public const string UserNotificationSettingsChanged = "Identity.UserNotificationSettingsChanged.v1";

    public const string BookCreated = "Catalog.BookCreated.v1";
    public const string BookUpdated = "Catalog.BookUpdated.v1";
    public const string BookPricingChanged = "Catalog.BookPricingChanged.v1";
    public const string BookPublished = "Catalog.BookPublished.v1";
    public const string BookUnlisted = "Catalog.BookUnlisted.v1";
    public const string BookRelisted = "Catalog.BookRelisted.v1";
    public const string BookRemoved = "Catalog.BookRemoved.v1";
    public const string AnnouncementPublished = "Catalog.AnnouncementPublished.v1";

    public const string CoverUploaded = "Content.CoverUploaded.v1";
    public const string EditionUploaded = "Content.EditionUploaded.v1";
    public const string ContentBlocked = "Content.ContentBlocked.v1";

    public const string OrderCreated = "Orders.OrderCreated.v1";
    public const string PaymentInitiated = "Orders.PaymentInitiated.v1";
    public const string OrderPaid = "Orders.OrderPaid.v1";
    public const string OrderCancelled = "Orders.OrderCancelled.v1";
    public const string OrderRefunded = "Orders.OrderRefunded.v1";

    public const string EntitlementGranted = "Library.EntitlementGranted.v1";
    public const string EntitlementRevoked = "Library.EntitlementRevoked.v1";
}
