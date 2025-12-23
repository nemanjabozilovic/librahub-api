namespace LibraHub.Identity.Domain.Tokens;

public class RegistrationCompletionToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public bool IsUsed => UsedAt.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    private RegistrationCompletionToken()
    { } // For EF Core

    public RegistrationCompletionToken(Guid id, Guid userId, string token, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }
}

