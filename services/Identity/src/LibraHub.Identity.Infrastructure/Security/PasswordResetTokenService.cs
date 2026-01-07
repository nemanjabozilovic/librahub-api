using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace LibraHub.Identity.Infrastructure.Security;

public class PasswordResetTokenService : IPasswordResetTokenService
{
    private readonly TokenOptions _options;

    public PasswordResetTokenService(IOptions<TokenOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public DateTime GetExpiration()
    {
        return DateTime.UtcNow.AddHours(_options.PasswordResetExpirationHours);
    }

    public int GetExpirationHours()
    {
        return _options.PasswordResetExpirationHours;
    }
}
