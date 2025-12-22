namespace LibraHub.Contracts.Catalog.V1;

public record BookPublishedV1
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}
