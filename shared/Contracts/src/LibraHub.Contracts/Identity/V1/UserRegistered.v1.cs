namespace LibraHub.Contracts.Identity.V1;

public record UserRegisteredV1
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
}
