namespace LibraHub.Identity.Application.Abstractions;

public interface IRegistrationCompletionTokenService
{
    string GenerateToken();

    DateTime GetExpiration();

    int GetExpirationHours();
}
