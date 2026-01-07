namespace LibraHub.Contracts.Catalog.V1;

public record BookRemovedV1
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid RemovedBy { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset RemovedAt { get; init; }
}
