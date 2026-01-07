namespace LibraHub.Library.Domain.Errors;

public static class LibraryErrors
{
    public static class Entitlement
    {
        public const string NotFound = "Entitlement not found";
        public const string AlreadyActive = "Entitlement is already active";
        public const string AlreadyRevoked = "Entitlement is already revoked";
        public const string CannotRevoke = "Entitlement cannot be revoked";
        public const string AlreadyExists = "Entitlement already exists for this user and book";
    }

    public static class Book
    {
        public const string NotFound = "Book not found";
        public const string NotAvailable = "Book is not available";
        public const string SnapshotNotFound = "Book snapshot not found";
    }

    public static class ReadingProgress
    {
        public const string NotFound = "Reading progress not found";
        public const string InvalidPercentage = "Progress percentage must be between 0 and 100";
    }

    public static class User
    {
        public const string NotAuthenticated = "User is not authenticated";
        public const string NotAuthorized = "User is not authorized to perform this action";
    }
}
