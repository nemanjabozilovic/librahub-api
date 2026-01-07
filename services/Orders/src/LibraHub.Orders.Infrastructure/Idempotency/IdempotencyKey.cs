namespace LibraHub.Orders.Infrastructure.Idempotency;

public class IdempotencyKey
{
    public string Key { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public byte[] ResponseBody { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
