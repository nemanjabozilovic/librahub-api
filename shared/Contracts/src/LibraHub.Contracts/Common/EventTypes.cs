namespace LibraHub.Contracts.Common;

public static class EventTypes
{
    // Identity events
    public const string UserRegistered = "Identity.UserRegistered.v1";

    public const string EmailVerified = "Identity.EmailVerified.v1";
    public const string UserDisabled = "Identity.UserDisabled.v1";
    public const string RoleAssigned = "Identity.RoleAssigned.v1";

    // Catalog events
    public const string BookCreated = "Catalog.BookCreated.v1";

    public const string BookUpdated = "Catalog.BookUpdated.v1";
    public const string BookPricingChanged = "Catalog.BookPricingChanged.v1";
    public const string BookPublished = "Catalog.BookPublished.v1";
    public const string BookUnlisted = "Catalog.BookUnlisted.v1";
    public const string BookRemoved = "Catalog.BookRemoved.v1";
    public const string AnnouncementPublished = "Catalog.AnnouncementPublished.v1";

    // Content events
    public const string CoverUploaded = "Content.CoverUploaded.v1";

    public const string EditionUploaded = "Content.EditionUploaded.v1";
}
