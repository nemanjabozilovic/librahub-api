namespace LibraHub.Library.Application.Reading.Queries.GetProgress;

public class ReadingProgressDto
{
    public Guid BookId { get; init; }
    public string? Format { get; init; }
    public int? Version { get; init; }
    public decimal Percentage { get; init; }
    public int? LastPage { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
}
