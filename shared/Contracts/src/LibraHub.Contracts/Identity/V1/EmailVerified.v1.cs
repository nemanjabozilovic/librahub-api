namespace LibraHub.Contracts.Identity.V1;

public record EmailVerifiedV1
{
    public Guid UserId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
