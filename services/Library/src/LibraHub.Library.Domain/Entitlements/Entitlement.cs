namespace LibraHub.Library.Domain.Entitlements;

public class Entitlement
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }
    public EntitlementStatus Status { get; private set; }
    public EntitlementSource Source { get; private set; }
    public DateTime AcquiredAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }
    public Guid? OrderId { get; private set; }

    protected Entitlement()
    {
    } // For EF Core

    public Entitlement(
        Guid id,
        Guid userId,
        Guid bookId,
        EntitlementSource source,
        Guid? orderId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (bookId == Guid.Empty)
            throw new ArgumentException("BookId cannot be empty", nameof(bookId));

        Id = id;
        UserId = userId;
        BookId = bookId;
        Status = EntitlementStatus.Active;
        Source = source;
        AcquiredAt = DateTime.UtcNow;
        OrderId = orderId;
    }

    public void Revoke(string? reason = null)
    {
        if (Status == EntitlementStatus.Revoked)
        {
            throw new InvalidOperationException("Entitlement is already revoked");
        }

        Status = EntitlementStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
    }

    public void Reactivate()
    {
        if (Status == EntitlementStatus.Active)
        {
            throw new InvalidOperationException("Entitlement is already active");
        }

        Status = EntitlementStatus.Active;
        RevokedAt = null;
        RevocationReason = null;
    }

    public bool IsActive => Status == EntitlementStatus.Active;
}
