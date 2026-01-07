namespace LibraHub.Contracts.Content.V1;

public record CoverUploadedV1
{
    public Guid BookId { get; init; }
    public string CoverRef { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }
}
