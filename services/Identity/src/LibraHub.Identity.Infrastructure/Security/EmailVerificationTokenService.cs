using LibraHub.Identity.Application.Abstractions;
using LibraHub.Identity.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace LibraHub.Identity.Infrastructure.Security;

public class EmailVerificationTokenService : IEmailVerificationTokenService
{
    private readonly TokenOptions _options;

    public EmailVerificationTokenService(IOptions<TokenOptions> options)
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
        return DateTime.UtcNow.AddDays(_options.EmailVerificationExpirationDays);
    }
}
