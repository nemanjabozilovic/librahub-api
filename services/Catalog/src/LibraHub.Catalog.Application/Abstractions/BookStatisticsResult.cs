namespace LibraHub.Catalog.Application.Abstractions;

public record BookStatisticsResult
{
    public int Total { get; init; }
    public int Published { get; init; }
    public int Draft { get; init; }
    public int Unlisted { get; init; }
    public int NewLast30Days { get; init; }
}

