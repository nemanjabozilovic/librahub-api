namespace LibraHub.Contracts.Catalog.V1;

public record BookPublishedV1
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public DateTimeOffset PublishedAt { get; init; }
}
