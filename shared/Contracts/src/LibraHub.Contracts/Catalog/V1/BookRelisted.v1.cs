namespace LibraHub.Contracts.Catalog.V1;

public record BookRelistedV1
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset RelistedAt { get; init; }
}
