using LibraHub.Identity.Domain.Users;

namespace LibraHub.Identity.Application.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();

    DateTimeOffset GetRefreshTokenExpiration();
}
