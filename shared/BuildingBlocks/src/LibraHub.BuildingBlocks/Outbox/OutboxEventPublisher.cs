using LibraHub.BuildingBlocks.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LibraHub.BuildingBlocks.Outbox;

public class OutboxEventPublisher<TDbContext> : IOutboxWriter where TDbContext : DbContext
{
    private readonly TDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public OutboxEventPublisher(TDbContext context)
    {
        _context = context;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task WriteAsync<T>(T integrationEvent, string eventType, CancellationToken cancellationToken = default) where T : class
    {
        var payload = JsonSerializer.Serialize(integrationEvent, _jsonOptions);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
