namespace LibraHub.Catalog.Domain.Errors;

public static class CatalogErrors
{
    public static class Book
    {
        public const string NotFound = "BOOK_NOT_FOUND";
        public const string AlreadyRemoved = "BOOK_ALREADY_REMOVED";
        public const string CannotPublish = "BOOK_CANNOT_BE_PUBLISHED";
        public const string CannotUnlist = "BOOK_CANNOT_BE_UNLISTED";
        public const string CannotRelist = "BOOK_CANNOT_BE_RELISTED";
        public const string CannotUpdate = "BOOK_CANNOT_BE_UPDATED";
        public const string MissingTitle = "BOOK_MISSING_TITLE";
        public const string MissingDescription = "BOOK_MISSING_DESCRIPTION";
        public const string MissingLanguage = "BOOK_MISSING_LANGUAGE";
        public const string InvalidPricing = "BOOK_INVALID_PRICING";
        public const string ContentNotReady = "BOOK_CONTENT_NOT_READY";
    }

    public static class Pricing
    {
        public const string NotFound = "PRICING_NOT_FOUND";
        public const string InvalidPrice = "PRICING_INVALID_PRICE";
        public const string InvalidPromoDates = "PRICING_INVALID_PROMO_DATES";
    }

    public static class Announcement
    {
        public const string NotFound = "ANNOUNCEMENT_NOT_FOUND";
        public const string AlreadyPublished = "ANNOUNCEMENT_ALREADY_PUBLISHED";
        public const string CannotUpdate = "ANNOUNCEMENT_CANNOT_BE_UPDATED";
        public const string MissingTitle = "ANNOUNCEMENT_MISSING_TITLE";
        public const string MissingContent = "ANNOUNCEMENT_MISSING_CONTENT";
    }
}
