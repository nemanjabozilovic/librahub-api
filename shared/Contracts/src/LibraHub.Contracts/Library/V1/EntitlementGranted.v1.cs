namespace LibraHub.Contracts.Library.V1;

public class EntitlementGrantedV1
{
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAtUtc { get; set; }
    public string BookTitle { get; set; } = string.Empty;
}
