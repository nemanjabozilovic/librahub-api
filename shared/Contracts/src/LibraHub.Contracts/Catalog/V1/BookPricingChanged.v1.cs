namespace LibraHub.Contracts.Catalog.V1;

public record BookPricingChangedV1
{
    public Guid BookId { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? VatRate { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
