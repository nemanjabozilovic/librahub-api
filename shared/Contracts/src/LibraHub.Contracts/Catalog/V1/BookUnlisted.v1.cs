namespace LibraHub.Contracts.Catalog.V1;

public record BookUnlistedV1
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime UnlistedAt { get; init; }
}
