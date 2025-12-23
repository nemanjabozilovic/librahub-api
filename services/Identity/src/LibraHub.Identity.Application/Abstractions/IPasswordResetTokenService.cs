namespace LibraHub.Identity.Application.Abstractions;

public interface IPasswordResetTokenService
{
    string GenerateToken();

    DateTime GetExpiration();

    int GetExpirationHours();
}

