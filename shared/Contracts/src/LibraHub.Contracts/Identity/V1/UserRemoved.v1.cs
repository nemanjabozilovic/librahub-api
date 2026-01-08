namespace LibraHub.Contracts.Identity.V1;

public record UserRemovedV1
{
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
}

