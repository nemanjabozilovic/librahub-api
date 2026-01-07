namespace LibraHub.Contracts.Catalog.V1;

public record BookUpdatedV1
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; }
}
