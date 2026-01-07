namespace LibraHub.Contracts.Content.V1;

public record EditionUploadedV1
{
    public Guid BookId { get; init; }
    public string Format { get; init; } = string.Empty; // PDF, EPUB, etc.
    public int Version { get; init; }
    public string EditionRef { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }
}
