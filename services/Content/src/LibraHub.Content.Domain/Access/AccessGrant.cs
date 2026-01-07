using LibraHub.Content.Domain.Books;

namespace LibraHub.Content.Domain.Access;

public class AccessGrant
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public Guid BookId { get; private set; }
    public BookFormat? Format { get; private set; }
    public int? Version { get; private set; }
    public AccessScope Scope { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    protected AccessGrant()
    { } // For EF Core

    public AccessGrant(
        Guid id,
        string token,
        Guid bookId,
        BookFormat? format,
        int? version,
        AccessScope scope,
        Guid userId,
        DateTime expiresAt)
    {
        Id = id;
        Token = token;
        BookId = bookId;
        Format = format;
        Version = version;
        Scope = scope;
        UserId = userId;
        IssuedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public void Revoke()
    {
        if (IsRevoked)
        {
            return;
        }

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public bool IsValid(DateTime now)
    {
        return !IsRevoked && now >= IssuedAt && now <= ExpiresAt;
    }

    public bool IsNearExpiry(DateTime now, TimeSpan threshold)
    {
        if (IsRevoked || now > ExpiresAt)
        {
            return false;
        }

        var timeUntilExpiry = ExpiresAt - now;
        return timeUntilExpiry <= threshold;
    }

    public void RefreshExpiry(DateTime newExpiresAt)
    {
        if (IsRevoked)
        {
            return;
        }

        if (newExpiresAt <= ExpiresAt)
        {
            return;
        }

        ExpiresAt = newExpiresAt;
    }
}
