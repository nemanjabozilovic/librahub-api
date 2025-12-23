namespace LibraHub.Notifications.Infrastructure.Idempotency;

public class IdempotencyKey
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public byte[] ResponseBody { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
}

