namespace LibraHub.Contracts.Common;

public class EventEnvelope<T>
{
    public string EventType { get; set; } = string.Empty;
    public string EventVersion { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public T Data { get; set; } = default!;
}
