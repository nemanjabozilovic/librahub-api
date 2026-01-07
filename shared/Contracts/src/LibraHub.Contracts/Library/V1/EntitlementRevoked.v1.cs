namespace LibraHub.Contracts.Library.V1;

public class EntitlementRevokedV1
{
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset RevokedAtUtc { get; set; }
}
