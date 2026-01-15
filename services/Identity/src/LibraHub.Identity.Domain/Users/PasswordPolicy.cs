using System.Text.RegularExpressions;

namespace LibraHub.Identity.Domain.Users;

public static class PasswordPolicy
{
    private const int MinLength = 8;
    private const int MaxLength = 128;

    public static bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < MinLength || password.Length > MaxLength)
            return false;

        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;

        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;

        if (!Regex.IsMatch(password, @"[0-9]"))
            return false;

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            return false;

        return true;
    }

    public static string GetPolicyDescription()
    {
        return $"Password must be between {MinLength} and {MaxLength} characters, and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.";
    }
}
