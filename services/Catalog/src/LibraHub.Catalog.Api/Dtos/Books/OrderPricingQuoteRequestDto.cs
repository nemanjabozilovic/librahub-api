namespace LibraHub.Catalog.Api.Dtos.Books;

public record OrderPricingQuoteRequestDto
{
    public List<Guid> BookIds { get; init; } = [];
    public Guid? UserId { get; init; }
    public DateTimeOffset? AtUtc { get; init; }
}
