using LibraHub.BuildingBlocks.Abstractions;

namespace LibraHub.BuildingBlocks;

public class Clock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
