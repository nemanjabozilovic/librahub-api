namespace LibraHub.Contracts.Catalog.V1;

public record BookCreatedV1
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
