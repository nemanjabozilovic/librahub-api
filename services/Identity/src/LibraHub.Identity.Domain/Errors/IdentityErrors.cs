namespace LibraHub.Identity.Domain.Errors;

public static class IdentityErrors
{
    public static class User
    {
        public const string NotFound = "USER_NOT_FOUND";
        public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
        public const string InvalidPassword = "INVALID_PASSWORD";
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string AccountRemoved = "ACCOUNT_REMOVED";
        public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
        public const string CannotRemoveLastAdmin = "CANNOT_REMOVE_LAST_ADMIN";
    }

    public static class Token
    {
        public const string InvalidToken = "INVALID_TOKEN";
        public const string ExpiredToken = "EXPIRED_TOKEN";
        public const string RevokedToken = "REVOKED_TOKEN";
        public const string TokenNotFound = "TOKEN_NOT_FOUND";
    }

    public static class Authentication
    {
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    }
}
