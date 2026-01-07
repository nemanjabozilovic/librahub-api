using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LibraHub.BuildingBlocks.Outbox;

public class OutboxPublisherWorkerHelper<TDbContext> : OutboxPublisherWorker where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;

    public OutboxPublisherWorkerHelper(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherWorkerHelper<TDbContext>> logger,
        IConnection connection,
        string exchangeName = "librahub.events") : base(logger, connection, exchangeName)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        return await context.Set<OutboxMessage>()
            .Where(om => om.ProcessedAt == null)
            .OrderBy(om => om.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    protected override async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await MarkAsProcessedBatchAsync([messageId], cancellationToken);
    }

    protected override async Task MarkAsProcessedBatchAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var idsList = messageIds.ToList();
        var messages = await context.Set<OutboxMessage>()
            .Where(om => idsList.Contains(om.Id))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var message in messages)
        {
            message.ProcessedAt = now;
        }

        if (messages.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    protected override async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var message = await context.Set<OutboxMessage>().FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.Error = error;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
