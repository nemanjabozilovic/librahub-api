namespace LibraHub.Content.Domain.Errors;

public static class ContentErrors
{
    public static class Book
    {
        public const string NotFound = "BOOK_NOT_FOUND";
        public const string Blocked = "BOOK_BLOCKED";
        public const string NotFree = "BOOK_NOT_FREE";
        public const string NotOwned = "BOOK_NOT_OWNED";
    }

    public static class Cover
    {
        public const string NotFound = "COVER_NOT_FOUND";
        public const string AlreadyExists = "COVER_ALREADY_EXISTS";
        public const string Blocked = "COVER_BLOCKED";
    }

    public static class Edition
    {
        public const string NotFound = "EDITION_NOT_FOUND";
        public const string Blocked = "EDITION_BLOCKED";
        public const string InvalidFormat = "EDITION_INVALID_FORMAT";
        public const string InvalidVersion = "EDITION_INVALID_VERSION";
    }

    public static class Access
    {
        public const string TokenInvalid = "ACCESS_TOKEN_INVALID";
        public const string TokenExpired = "ACCESS_TOKEN_EXPIRED";
        public const string TokenRevoked = "ACCESS_TOKEN_REVOKED";
        public const string AccessDenied = "ACCESS_DENIED";
    }

    public static class Storage
    {
        public const string UploadFailed = "STORAGE_UPLOAD_FAILED";
        public const string DownloadFailed = "STORAGE_DOWNLOAD_FAILED";
        public const string InvalidFileType = "STORAGE_INVALID_FILE_TYPE";
        public const string FileTooLarge = "STORAGE_FILE_TOO_LARGE";
    }
}
