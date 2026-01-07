namespace LibraHub.Contracts.Content.V1;

public record ContentBlockedV1
{
    public Guid BookId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset BlockedAt { get; init; }
}
