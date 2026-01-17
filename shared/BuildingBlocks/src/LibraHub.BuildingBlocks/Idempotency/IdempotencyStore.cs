using Microsoft.EntityFrameworkCore;

namespace LibraHub.BuildingBlocks.Idempotency;

public class IdempotencyStore<TDbContext, TIdempotencyKey>(TDbContext context) : IIdempotencyStore
    where TDbContext : DbContext
    where TIdempotencyKey : class, new()
{
    private readonly TDbContext _context = context;

    public async Task<IdempotencyResponse?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = await _context.Set<TIdempotencyKey>()
            .Where(k => EF.Property<string>(k, "Key") == idempotencyKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (key == null)
        {
            return null;
        }

        return new IdempotencyResponse
        {
            StatusCode = EF.Property<int>(key, "StatusCode"),
            ContentType = EF.Property<string>(key, "ContentType") ?? string.Empty,
            Body = EF.Property<byte[]>(key, "ResponseBody") ?? Array.Empty<byte>()
        };
    }

    public async Task StoreResponseAsync(string idempotencyKey, int statusCode, string contentType, byte[] body, CancellationToken cancellationToken = default)
    {
        var key = new TIdempotencyKey();
        var entry = _context.Entry(key);

        entry.Property("Key").CurrentValue = idempotencyKey;
        entry.Property("StatusCode").CurrentValue = statusCode;
        entry.Property("ContentType").CurrentValue = contentType;
        entry.Property("ResponseBody").CurrentValue = body;
        entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;

        await _context.Set<TIdempotencyKey>().AddAsync(key, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
