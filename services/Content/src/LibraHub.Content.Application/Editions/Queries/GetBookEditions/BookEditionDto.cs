namespace LibraHub.Content.Application.Editions.Queries.GetBookEditions;

public record BookEditionDto
{
    public Guid Id { get; init; }
    public string Format { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
