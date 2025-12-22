namespace LibraHub.Contracts.Content.V1;

public record EditionUploadedV1
{
    public Guid BookId { get; init; }
    public string Format { get; init; } = string.Empty; // PDF, EPUB, etc.
    public DateTime UploadedAt { get; init; }
}
