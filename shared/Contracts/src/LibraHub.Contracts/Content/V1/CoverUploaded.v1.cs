namespace LibraHub.Contracts.Content.V1;

public record CoverUploadedV1
{
    public Guid BookId { get; init; }
    public string CoverRef { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}
